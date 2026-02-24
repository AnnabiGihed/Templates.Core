using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Protocols;
using Templates.Core.Authentication.Events;
using Microsoft.IdentityModel.JsonWebTokens;
using Templates.Core.Authentication.Responses;
using Templates.Core.Authentication.Blazor.Storage;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Templates.Core.Authentication.Blazor.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Full Keycloak authentication service for Blazor Server.
///
///              Security model — no tokens in the browser:
///              - PKCE code verifier, OAuth2 state, nonce and tokens are stored ONLY in Redis.
///              - The browser receives a single, opaque, HttpOnly session cookie.
///              - Login triggers a server-to-Keycloak redirect; the callback page calls
///                HandleCallbackAsync to complete the exchange.
///              - Token validation uses Keycloak's JWKS endpoint for full signature verification.
///              - Nonce is validated against the ID token to prevent replay attacks.
///              - ForceRefreshAsync enables safe 401 retry without looping.
/// </summary>
public sealed class KeycloakAuthService : IBlazorKeycloakAuthService
{
	#region Constants
	internal const string SessionCookieName = "kc_session";
	private const int SessionIdBytes = 32;
	#endregion

	#region Dependencies
	private readonly HttpClient _http;
	private readonly NavigationManager _nav;
	private readonly KeycloakOptions _options;
	private readonly ILogger<KeycloakAuthService> _logger;
	private readonly IBlazorTokenSessionStore _sessionStore;
	private readonly IHttpContextAccessor _httpContextAccessor;
	#endregion

	#region Circuit-scoped state (one instance per Blazor circuit)
	private string? _sessionId;
	private ClaimsPrincipal? _user;
	private BlazorTokenSession? _session;
	/// <summary>
	/// Cached Keycloak OIDC configuration (signing keys).
	/// </summary>
	private OpenIdConnectConfiguration? _oidcConfig;
	private readonly SemaphoreSlim _oidcLock = new(1, 1);
	private readonly SemaphoreSlim _refreshLock = new(1, 1);
	#endregion

	#region Public API
	public ClaimsPrincipal? User => _user;
	public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;
	public bool IsAuthenticated => _session is not null && _session.HasTokens && !_session.IsExpired;
	#endregion

	#region Constructor
	public KeycloakAuthService(	IOptions<KeycloakOptions> options, IBlazorTokenSessionStore sessionStore, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, NavigationManager nav, ILogger<KeycloakAuthService> logger)
	{
		_nav = nav;
		_logger = logger;
		_options = options.Value;
		_options.Validate();
		_sessionStore = sessionStore;
		_httpContextAccessor = httpContextAccessor;
		_http = httpClientFactory.CreateClient(nameof(KeycloakAuthService));
	}
	#endregion

	#region Login — server-side PKCE, redirect to Keycloak
	/// <inheritdoc />
	public async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		try
		{
			var sessionId = GenerateSessionId();
			var (codeVerifier, codeChallenge) = GeneratePkce();
			var state = GenerateRandom();
			var nonce = GenerateRandom();
			var returnUrl = _nav.Uri;

			var flowSession = new BlazorTokenSession
			{
				PkceVerifier = codeVerifier,
				OAuthState = state,
				Nonce = nonce,
				ReturnUrl = returnUrl
			};

			await _sessionStore.SaveAsync(sessionId, flowSession, ct);

			// Try to write the cookie now (works in SSR / Razor Page context).
			// If the response is already committed (interactive Blazor Server circuit),
			// we fall back to passing the session ID through the OAuth2 state parameter
			// so the callback page can write the cookie on the returning request,
			// which is always a fresh HTTP GET where headers are not yet committed.
			var ctx = _httpContextAccessor.HttpContext;
			if (ctx is not null && !ctx.Response.HasStarted)
			{
				SetSessionCookie(sessionId, persistent: false);
			}
			// else: sessionId is embedded in state below; HandleCallbackAsync recovers it.

			var redirectUri = BuildCallbackUri();
			// Encode sessionId into state so the callback can recover it when the
			// cookie could not be written upfront: state = "<oauthState>.<sessionId>"
			var stateWithSession = $"{state}.{sessionId}";
			var authUrl = BuildAuthUrl(codeChallenge, stateWithSession, nonce, redirectUri);

			_nav.NavigateTo(authUrl, forceLoad: true);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Keycloak: failed to initiate login.");
			return false;
		}
	}
	#endregion

	#region Callback — exchange code, validate, persist
	/// <inheritdoc />
	public async Task<bool> HandleCallbackAsync(string code, string returnedState, CancellationToken ct = default)
	{
		try
		{
			// State format: "<oauthState>.<sessionId>"
			// Split on the last dot so the oauthState portion (which may itself
			// contain dots in theory) is preserved correctly.
			var lastDot = returnedState.LastIndexOf('.');
			if (lastDot < 0)
			{
				_logger.LogWarning("Keycloak: malformed state parameter — missing session ID segment.");
				return false;
			}

			var oauthState = returnedState[..lastDot];
			var sessionIdFromState = returnedState[(lastDot + 1)..];

			// Prefer the session ID from the HttpOnly cookie (set when the response
			// was not yet committed). Fall back to the value embedded in state
			// (set when LoginAsync ran inside a committed Blazor circuit).
			var sessionId = ReadSessionIdFromCookie() ?? sessionIdFromState;

			if (string.IsNullOrEmpty(sessionId))
			{
				_logger.LogWarning("Keycloak: could not determine session ID from cookie or state.");
				return false;
			}

			var flowSession = await _sessionStore.GetAsync(sessionId, ct);
			if (flowSession is null)
			{
				_logger.LogWarning("Keycloak: callback session not found in Redis (expired or tampered).");
				return false;
			}

			// CSRF — validate the oauth state portion only
			if (string.IsNullOrEmpty(flowSession.OAuthState) || flowSession.OAuthState != oauthState)
			{
				_logger.LogWarning("Keycloak: OAuth2 state mismatch — possible CSRF attack.");
				await _sessionStore.RemoveAsync(sessionId, ct);
				return false;
			}

			if (string.IsNullOrEmpty(flowSession.PkceVerifier) || string.IsNullOrEmpty(flowSession.Nonce))
			{
				_logger.LogWarning("Keycloak: PKCE verifier or nonce missing from session.");
				await _sessionStore.RemoveAsync(sessionId, ct);
				return false;
			}

			var tokenResponse = await ExchangeCodeAsync(code, flowSession.PkceVerifier, BuildCallbackUri(), ct);

			if (tokenResponse.IdToken is not null)
				await ValidateIdTokenNonceAsync(tokenResponse.IdToken, flowSession.Nonce, ct);

			var principal = await ValidateAndParseAccessTokenAsync(tokenResponse.AccessToken, ct);

			var refreshExpiresAt = tokenResponse.RefreshExpiresIn > 0
				? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.RefreshExpiresIn)
				: (DateTimeOffset?)null;

			flowSession.AccessToken = tokenResponse.AccessToken;
			flowSession.RefreshToken = tokenResponse.RefreshToken;
			flowSession.IdToken = tokenResponse.IdToken;
			flowSession.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
			flowSession.RefreshTokenExpiresAt = refreshExpiresAt;
			flowSession.PkceVerifier = null;
			flowSession.OAuthState = null;
			flowSession.Nonce = null;

			await _sessionStore.SaveAsync(sessionId, flowSession, ct);

			// The callback is always a fresh HTTP GET — safe to write the cookie here
			// regardless of whether it was written during LoginAsync.
			SetSessionCookie(sessionId, persistent: true);

			_sessionId = sessionId;
			_session = flowSession;
			_user = principal;

			NotifyAuthStateChanged(isAuthenticated: true);

			_logger.LogInformation("Keycloak: user {Username} logged in via Blazor.",
				_user.FindFirst("preferred_username")?.Value);

			return true;
		}
		catch (SecurityTokenValidationException ex)
		{
			_logger.LogError(ex, "Keycloak: token validation failed during callback.");
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Keycloak: callback token exchange failed.");
			return false;
		}
	}
	#endregion

		#region Logout
		/// <inheritdoc />
	public async Task LogoutAsync(CancellationToken ct = default)
	{
		var current = _session;
		var sessionId = _sessionId;

		_session = null;
		_user = null;
		_sessionId = null;

		NotifyAuthStateChanged(isAuthenticated: false);

		if (sessionId is not null)
			await _sessionStore.RemoveAsync(sessionId, ct);

		if (current?.RefreshToken is not null)
		{
			try { await RevokeTokenAsync(current.RefreshToken, ct); }
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Keycloak: refresh token revocation failed (ignored).");
			}
		}

		DeleteSessionCookie();

		if (current?.IdToken is not null)
		{
			var postLogoutUri = Uri.EscapeDataString(_nav.BaseUri.TrimEnd('/'));
			var endSessionUrl = $"{_options.LogoutUrl}" + $"?id_token_hint={Uri.EscapeDataString(current.IdToken)}" + $"&post_logout_redirect_uri={postLogoutUri}";

			_nav.NavigateTo(endSessionUrl, forceLoad: true);
		}

		_logger.LogInformation("Keycloak: user logged out.");
	}
	#endregion

	#region Get / force refresh access token
	/// <inheritdoc />
	public async Task<string> ForceRefreshAsync(CancellationToken ct = default)
	{
		if (_session?.CanRefresh != true)
			throw new UnauthorizedAccessException("No valid refresh token available.");

		return await RefreshInternalAsync(force: true, ct);
	}

	/// <inheritdoc />
	public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
	{
		if (_session is null || !_session.HasTokens)
			throw new UnauthorizedAccessException("Not logged in.");

		if (!_session.IsExpired)
			return _session.AccessToken!;

		if (!_session.CanRefresh)
			throw new UnauthorizedAccessException("Session expired. Please log in again.");

		return await RefreshInternalAsync(force: false, ct);
	}
	#endregion

	#region Restore session from cookie
	private async Task<bool> InitialiseFromSessionAsync(CancellationToken ct)
	{
		try
		{
			var sessionId = ReadSessionIdFromCookie();
			if (sessionId is null)
				return false;

			var session = await _sessionStore.GetAsync(sessionId, ct);
			if (session is null || !session.HasTokens)
				return false;

			if (!session.IsExpired)
			{
				_sessionId = sessionId;
				_session = session;
				_user = await ValidateAndParseAccessTokenAsync(session.AccessToken!, ct);
				NotifyAuthStateChanged(isAuthenticated: true);
				_logger.LogDebug("Keycloak: Blazor session restored from Redis.");
				return true;
			}

			if (session.CanRefresh)
			{
				_sessionId = sessionId;
				_session = session;
				await RefreshInternalAsync(force: true, ct);
				_logger.LogInformation("Keycloak: Blazor session refreshed on restore.");
				return true;
			}

			_logger.LogInformation("Keycloak: stored Blazor session expired and cannot be refreshed.");
			await _sessionStore.RemoveAsync(sessionId, ct);
			DeleteSessionCookie();
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Keycloak: failed to restore Blazor session.");
			return false;
		}
	}

	/// <inheritdoc />
	public async Task InitialiseFromCookieAsync(CancellationToken ct = default)
	{
		await InitialiseFromSessionAsync(ct);
	}

	/// <inheritdoc />
	public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
	{
		return await InitialiseFromSessionAsync(ct);
	}
	#endregion

	#region Internal refresh
	private void NotifyAuthStateChanged(bool isAuthenticated)
	{
		AuthStateChanged?.Invoke(
			this, new AuthStateChangedEventArgs(isAuthenticated, _user));
	}

	private async Task<string> RefreshInternalAsync(bool force, CancellationToken ct)
	{
		await _refreshLock.WaitAsync(ct);
		try
		{
			if (!force && _session is not null && !_session.IsExpired)
				return _session.AccessToken!;

			if (_session?.RefreshToken is null || !_session.CanRefresh)
				throw new UnauthorizedAccessException("No valid refresh token available.");

			var tokenResponse = await RefreshTokenGrantAsync(_session.RefreshToken, ct);

			var refreshExpiresAt = tokenResponse.RefreshExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.RefreshExpiresIn) : _session.RefreshTokenExpiresAt;

			_session.AccessToken = tokenResponse.AccessToken;
			_session.RefreshToken = tokenResponse.RefreshToken ?? _session.RefreshToken;
			_session.IdToken = tokenResponse.IdToken ?? _session.IdToken;
			_session.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
			_session.RefreshTokenExpiresAt = refreshExpiresAt;

			_user = await ValidateAndParseAccessTokenAsync(tokenResponse.AccessToken, ct);

			if (_sessionId is not null)
				await _sessionStore.SaveAsync(_sessionId, _session, ct);

			NotifyAuthStateChanged(isAuthenticated: true);
			return _session.AccessToken;
		}
		finally
		{
			_refreshLock.Release();
		}
	}
	#endregion

	#region Token exchange (Keycloak token endpoint)
	private async Task RevokeTokenAsync(string refreshToken, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["token"] = refreshToken,
			["client_id"] = _options.ClientId,
			["token_type_hint"] = "refresh_token"
		});

		await _http.PostAsync(
			$"{_options.IssuerUrl}/protocol/openid-connect/revoke", body, ct);
	}
	private Task<KeycloakTokenResponse> RefreshTokenGrantAsync(string refreshToken, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
			["client_id"] = _options.ClientId
		});

		return PostToTokenEndpointAsync(body, ct);
	}
	private async Task<KeycloakTokenResponse> PostToTokenEndpointAsync(FormUrlEncodedContent body, CancellationToken ct)
	{
		var response = await _http.PostAsync(_options.TokenUrl, body, ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException($"Keycloak token endpoint returned {response.StatusCode}: {error}");
		}

		return await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct) ?? throw new InvalidOperationException(
				"Keycloak returned an empty token response.");
	}
	private Task<KeycloakTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["code"] = code,
			["redirect_uri"] = redirectUri,
			["code_verifier"] = codeVerifier,
			["client_id"] = _options.ClientId,
			["grant_type"] = "authorization_code"
		});

		return PostToTokenEndpointAsync(body, ct);
	}
	#endregion

	#region Cryptographic JWT validation
	/// <summary>
	/// Lazily fetches Keycloak's OIDC discovery document (with JWKS signing keys) and caches it
	/// for the lifetime of the circuit. A real production deployment should add a background
	/// refresh for key rotation; for most use-cases Keycloak's keys are very long-lived.
	/// </summary>
	private async Task<OpenIdConnectConfiguration> GetOidcConfigurationAsync(CancellationToken ct)
	{
		if (_oidcConfig is not null)
			return _oidcConfig;

		await _oidcLock.WaitAsync(ct);
		try
		{
			if (_oidcConfig is not null)
				return _oidcConfig;

			var docRetriever = new HttpDocumentRetriever(_http)
			{
				RequireHttps = _options.RequireHttpsMetadata
			};

			_oidcConfig = await OpenIdConnectConfigurationRetriever.GetAsync(_options.MetadataUrl, docRetriever, ct);

			_logger.LogDebug("Keycloak OIDC configuration loaded. Keys: {Count}", _oidcConfig.SigningKeys.Count());
		}
		finally
		{
			_oidcLock.Release();
		}

		return _oidcConfig;
	}

	/// <summary>
	/// Validates the ID token's signature and checks the nonce claim for replay protection.
	/// </summary>
	private async Task ValidateIdTokenNonceAsync(string idToken, string expectedNonce, CancellationToken ct)
	{
		var config = await GetOidcConfigurationAsync(ct);

		var parameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = _options.IssuerUrl,
			ValidateAudience = true,
			ValidAudience = _options.ClientId,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(30),
			IssuerSigningKeys = config.SigningKeys
		};

		var handler = new JsonWebTokenHandler();
		var result = await handler.ValidateTokenAsync(idToken, parameters);

		if (!result.IsValid)
		{
			_logger.LogWarning(result.Exception, "Keycloak: ID token signature validation failed.");
			throw new SecurityTokenValidationException("ID token failed cryptographic validation.", result.Exception);
		}

		if (!result.Claims.TryGetValue("nonce", out var actualNonce)
			|| actualNonce?.ToString() != expectedNonce)
		{
			_logger.LogWarning("Keycloak: ID token nonce mismatch — possible replay attack.");
			throw new SecurityTokenValidationException("ID token nonce validation failed.");
		}
	}

	/// <summary>
	/// Validates the access token's signature against Keycloak's JWKS, checks issuer,
	/// audience and lifetime. Returns a principal with flattened role claims.
	/// </summary>
	private async Task<ClaimsPrincipal> ValidateAndParseAccessTokenAsync(string accessToken, CancellationToken ct)
	{
		var config = await GetOidcConfigurationAsync(ct);

		var parameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = _options.IssuerUrl,
			ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
			ValidAudience = _options.Audience,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(30),
			IssuerSigningKeys = config.SigningKeys,
			NameClaimType = "preferred_username",
			RoleClaimType = ClaimTypes.Role
		};

		var handler = new JsonWebTokenHandler();
		var result = await handler.ValidateTokenAsync(accessToken, parameters);

		if (!result.IsValid)
		{
			_logger.LogError(result.Exception, "Keycloak: access token signature validation failed.");
			throw new SecurityTokenValidationException("Access token failed cryptographic validation.", result.Exception);
		}

		var claims = result.ClaimsIdentity.Claims.ToList();
		var identity = new ClaimsIdentity(claims, "keycloak", "preferred_username", ClaimTypes.Role);

		FlattenKeycloakRoles(identity);

		return new ClaimsPrincipal(identity);
	}
	#endregion

	#region Keycloak role flattening
	private static void FlattenKeycloakRoles(ClaimsIdentity identity)
	{
		var realmClaim = identity.FindFirst("realm_access");
		if (realmClaim is not null)
		{
			try
			{
				using var doc = JsonDocument.Parse(realmClaim.Value);
				if (doc.RootElement.TryGetProperty("roles", out var roles))
					foreach (var r in roles.EnumerateArray())
						if (r.GetString() is { } rv)
							identity.AddClaim(new Claim(ClaimTypes.Role, rv));
			}
			catch 
			{
			}
		}

		var resourceClaim = identity.FindFirst("resource_access");
		if (resourceClaim is not null)
		{
			try
			{
				using var doc = JsonDocument.Parse(resourceClaim.Value);
				foreach (var client in doc.RootElement.EnumerateObject())
					if (client.Value.TryGetProperty("roles", out var roles))
						foreach (var r in roles.EnumerateArray())
							if (r.GetString() is { } rv)
								identity.AddClaim(new Claim(ClaimTypes.Role, rv));
			}
			catch 
			{
			}
		}
	}
	#endregion

	#region Cookie management
	private void DeleteSessionCookie()
	{
		var ctx = _httpContextAccessor.HttpContext;
		if (ctx is null) return;

		ctx.Response.Cookies.Delete(SessionCookieName);
	}
	private string? ReadSessionIdFromCookie()
	{
		var ctx = _httpContextAccessor.HttpContext;
		return ctx?.Request.Cookies.TryGetValue(SessionCookieName, out var id) == true ? id : null;
	}
	private void SetSessionCookie(string sessionId, bool persistent)
	{
		var ctx = _httpContextAccessor.HttpContext;
		if (ctx is null) return;

		ctx.Response.Cookies.Append(SessionCookieName, sessionId, new CookieOptions
		{
			Secure = true,
			HttpOnly = true,
			IsEssential = true,
			SameSite = SameSiteMode.Lax,
			Expires = persistent ? DateTimeOffset.UtcNow.AddDays(1) : null
		});
	}
	#endregion

	#region URL builders
	private string BuildCallbackUri()
	{
		return $"{_nav.BaseUri.TrimEnd('/')}/auth/callback";
	}
	private string BuildAuthUrl(string codeChallenge, string state, string nonce, string redirectUri)
	{
		var scopes = Uri.EscapeDataString(_options.Scopes);
		return $"{_options.AuthorizationUrl}"
			 + $"?client_id={Uri.EscapeDataString(_options.ClientId)}"
			 + $"&response_type=code"
			 + $"&scope={scopes}"
			 + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
			 + $"&state={state}"
			 + $"&nonce={nonce}"
			 + $"&code_challenge={codeChallenge}"
			 + $"&code_challenge_method=S256";
	}
	#endregion

	#region Crypto helpers
	private static string GenerateRandom()
	{
		return Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
	}
	private static string GenerateSessionId()
	{
		return Base64UrlEncode(RandomNumberGenerator.GetBytes(SessionIdBytes));
	}
	private static string Base64UrlEncode(byte[] input)
	{
		return Convert.ToBase64String(input)
				.TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}
	private static (string verifier, string challenge) GeneratePkce()
	{
		var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
		var challenge = Base64UrlEncode(hash);
		return (verifier, challenge);
	}
	#endregion
}
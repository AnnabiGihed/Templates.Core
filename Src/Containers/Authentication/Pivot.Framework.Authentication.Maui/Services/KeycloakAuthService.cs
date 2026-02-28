using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pivot.Framework.Authentication.Events;
using Pivot.Framework.Authentication.Helpers;
using Pivot.Framework.Authentication.Responses;
using Pivot.Framework.Authentication.Services;
using Pivot.Framework.Authentication.Storage;

namespace Pivot.Framework.Authentication.Maui.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Full Keycloak authentication service for .NET MAUI Blazor Hybrid.
///              Implements Authorization Code + PKCE via <see cref="WebAuthenticator"/> and provides:
///              - Login (browser redirect → code exchange)
///              - Silent and forced refresh via refresh_token grant
///              - Cryptographic JWT validation against Keycloak's JWKS endpoint
///              - Secure token persistence in OS secure storage
///              - Logout (revoke + end-session)
///              - Claims extraction from the validated access token
///              - Nonce validation for replay protection
/// </summary>
public sealed class KeycloakAuthService : IKeycloakAuthService
{
	#region Dependencies
	private readonly HttpClient _http;
	private readonly KeycloakOptions _options;
	private readonly IKeycloakTokenStorage _storage;
	private readonly ILogger<KeycloakAuthService> _logger;
	#endregion

	#region State
	private ClaimsPrincipal? _user;
	private KeycloakTokenSet? _current;
	private readonly SemaphoreSlim _refreshLock = new(1, 1);

	/// <summary>
	/// Cached OIDC configuration including the JWKS signing keys.
	/// </summary>
	private OpenIdConnectConfiguration? _oidcConfig;
	private readonly SemaphoreSlim _oidcLock = new(1, 1);
	#endregion

	#region Public API
	public ClaimsPrincipal? User => _user;
	public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;
	public bool IsAuthenticated => _current is not null && !_current.IsExpired;
	#endregion

	#region Constructor
	public KeycloakAuthService(IOptions<KeycloakOptions> options, IKeycloakTokenStorage storage, IHttpClientFactory httpClientFactory, ILogger<KeycloakAuthService> logger)
	{
		_options = options.Value;
		_options.Validate();
		_storage = storage;
		_http = httpClientFactory.CreateClient(nameof(KeycloakAuthService));
		_logger = logger;
	}
	#endregion

	#region Login
	/// <inheritdoc />
	public async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		try
		{
			var (codeVerifier, codeChallenge) = GeneratePkce();
			var state = GenerateState();
			var nonce = GenerateState();
			var redirectUri = $"{_options.ClientId}://callback";

			var authUrl = new Uri(BuildAuthUrl(codeChallenge, state, nonce, redirectUri));

			var result = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions
			{
				Url = authUrl,
				CallbackUrl = new Uri(redirectUri),
				PrefersEphemeralWebBrowserSession = false
			});

			if (result is null || !result.Properties.TryGetValue("code", out var code))
			{
				_logger.LogWarning("Keycloak: authorization was cancelled or returned no code.");
				return false;
			}

			if (!result.Properties.TryGetValue("state", out var returnedState) || returnedState != state)
			{
				_logger.LogWarning("Keycloak: OAuth2 state mismatch — possible CSRF.");
				return false;
			}

			var tokens = await ExchangeCodeAsync(code, codeVerifier, redirectUri, ct);

			if (tokens.IdToken is not null)
				await ValidateNonceInIdTokenAsync(tokens.IdToken, nonce, ct);

			await PersistAndNotify(tokens, ct);

			_logger.LogInformation("Keycloak: user {Username} logged in.", _user?.FindFirst("preferred_username")?.Value);

			return true;
		}
		catch (TaskCanceledException)
		{
			_logger.LogInformation("Keycloak: login cancelled by user.");
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Keycloak: login failed.");
			return false;
		}
	}
	#endregion

	#region Logout
	/// <inheritdoc />
	public async Task LogoutAsync(CancellationToken ct = default)
	{
		var current = _current;

		_current = null;
		_user = null;
		await _storage.ClearAsync(ct);
		NotifyAuthStateChanged(isAuthenticated: false);

		if (current?.RefreshToken is not null)
		{
			try { await RevokeTokenAsync(current.RefreshToken, ct); }
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Keycloak: failed to revoke refresh token (ignored).");
			}
		}

		if (current?.IdToken is not null)
		{
			try
			{
				var endSessionUrl = $"{_options.LogoutUrl}" + $"?id_token_hint={Uri.EscapeDataString(current.IdToken)}" + $"&post_logout_redirect_uri={Uri.EscapeDataString($"{_options.ClientId}://loggedout")}";

				await Browser.Default.OpenAsync(endSessionUrl, BrowserLaunchMode.SystemPreferred);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Keycloak: failed to open end-session URL (ignored).");
			}
		}

		_logger.LogInformation("Keycloak: user logged out.");
	}
	#endregion

	#region Get / force refresh access token
	/// <inheritdoc />
	public async Task<string> ForceRefreshAsync(CancellationToken ct = default)
	{
		if (_current?.RefreshToken is null || !_current.CanRefresh)
			throw new UnauthorizedAccessException("No valid refresh token available.");

		return await RefreshInternalAsync(force: true, ct);
	}

	/// <inheritdoc />
	public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
	{
		if (_current is null)
			throw new UnauthorizedAccessException("Not logged in.");

		if (!_current.IsExpired)
			return _current.AccessToken;

		if (!_current.CanRefresh)
			throw new UnauthorizedAccessException("Session expired. Please log in again.");

		return await RefreshInternalAsync(force: false, ct);
	}
	#endregion

	#region Restore session
	/// <inheritdoc />
	public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
	{
		try
		{
			var stored = await _storage.GetAsync(ct);
			if (stored is null)
				return false;

			if (!stored.IsExpired)
			{
				await PersistAndNotify(stored, ct);
				_logger.LogInformation("Keycloak: session restored from storage.");
				return true;
			}

			if (stored.CanRefresh)
			{
				_current = stored;
				await RefreshInternalAsync(force: true, ct);
				_logger.LogInformation("Keycloak: session refreshed on restore.");
				return true;
			}

			_logger.LogInformation("Keycloak: stored session expired and refresh token is gone or expired.");
			await _storage.ClearAsync(ct);
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Keycloak: failed to restore session.");
			return false;
		}
	}
	#endregion

	#region Internal refresh
	private void NotifyAuthStateChanged(bool isAuthenticated)
	{
		AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(isAuthenticated, _user));
	}
	private async Task<string> RefreshInternalAsync(bool force, CancellationToken ct)
	{
		await _refreshLock.WaitAsync(ct);
		try
		{
			if (!force && _current is not null && !_current.IsExpired)
				return _current.AccessToken;

			if (_current?.RefreshToken is null || !_current.CanRefresh)
				throw new UnauthorizedAccessException("No valid refresh token available.");

			var tokens = await RefreshTokenGrantAsync(_current.RefreshToken, ct);
			await PersistAndNotify(tokens, ct);
			return tokens.AccessToken;
		}
		finally
		{
			_refreshLock.Release();
		}
	}
	private async Task PersistAndNotify(KeycloakTokenSet tokens, CancellationToken ct)
	{
		_user = await ValidateAndParseAccessTokenAsync(tokens.AccessToken, ct);
		_current = tokens;
		await _storage.SaveAsync(tokens, ct);
		NotifyAuthStateChanged(isAuthenticated: true);
	}
	#endregion

	#region Token exchange
	private async Task RevokeTokenAsync(string refreshToken, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["token"] = refreshToken,
			["client_id"] = _options.ClientId,
			["token_type_hint"] = "refresh_token"
		});

		await _http.PostAsync($"{_options.IssuerUrl}/protocol/openid-connect/revoke", body, ct);
	}
	private Task<KeycloakTokenSet> RefreshTokenGrantAsync(string refreshToken, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
			["client_id"] = _options.ClientId
		});

		return PostToTokenEndpointAsync(body, ct);
	}
	private async Task<KeycloakTokenSet> PostToTokenEndpointAsync(FormUrlEncodedContent body, CancellationToken ct)
	{
		var response = await _http.PostAsync(_options.TokenUrl, body, ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException($"Keycloak token endpoint returned {response.StatusCode}: {error}");
		}

		var json = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct) ?? throw new InvalidOperationException("Keycloak returned an empty token response.");

		DateTimeOffset? refreshExpiresAt = json.RefreshExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(json.RefreshExpiresIn) : null;

		return new KeycloakTokenSet
		{
			IdToken = json.IdToken,
			AccessToken = json.AccessToken,
			RefreshToken = json.RefreshToken,
			ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn),
			RefreshTokenExpiresAt = refreshExpiresAt
		};
	}
	private Task<KeycloakTokenSet> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken ct)
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
	/// Lazily fetches and caches the Keycloak OIDC configuration (including JWKS).
	/// Refresh is triggered at most once per concurrent call group.
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

			var retriever = new OpenIdConnectConfigurationRetriever();
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
	/// Validates the nonce claim inside the ID token to prevent replay attacks.
	/// Does NOT re-validate the full ID token signature here (the access token
	/// validation above already proved the token endpoint is authentic).
	/// </summary>
	private async Task ValidateNonceInIdTokenAsync(string idToken, string expectedNonce, CancellationToken ct)
	{
		var config = await GetOidcConfigurationAsync(ct);

		var parameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidIssuer = _options.IssuerUrl,
			ValidAudience = _options.ClientId,
			ClockSkew = TimeSpan.FromSeconds(30),
			IssuerSigningKeys = config.SigningKeys
		};

		var handler = new JsonWebTokenHandler();
		var result = await handler.ValidateTokenAsync(idToken, parameters);

		if (!result.IsValid)
		{
			_logger.LogWarning(
				result.Exception, "Keycloak: ID token signature validation failed.");
			throw new SecurityTokenValidationException("ID token failed cryptographic validation.", result.Exception);
		}

		if (!result.Claims.TryGetValue("nonce", out var actualNonce) || actualNonce?.ToString() != expectedNonce)
		{
			_logger.LogWarning("Keycloak: ID token nonce mismatch — possible replay attack.");
			throw new SecurityTokenValidationException("ID token nonce validation failed.");
		}
	}

	/// <summary>
	/// Fetches (and caches) Keycloak's OIDC discovery document and JWKS signing keys,
	/// then validates the access token's signature, issuer, audience, and lifetime.
	/// Returns a <see cref="ClaimsPrincipal"/> with flattened realm + client roles.
	/// </summary>
	private async Task<ClaimsPrincipal> ValidateAndParseAccessTokenAsync(string accessToken, CancellationToken ct)
	{
		var config = await GetOidcConfigurationAsync(ct);

		var parameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateLifetime = true,
			RoleClaimType = ClaimTypes.Role,
			ValidIssuer = _options.IssuerUrl,
			ValidAudience = _options.Audience,
			ClockSkew = TimeSpan.FromSeconds(30),
			NameClaimType = "preferred_username",
			IssuerSigningKeys = config.SigningKeys,
			ValidateAudience = !string.IsNullOrEmpty(_options.Audience)
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

		KeycloakRoleHelper.FlattenRoles(identity, _logger);

		return new ClaimsPrincipal(identity);
	}
	#endregion

	#region PKCE + state helpers
	private static string GenerateState()
	{
		return Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
	}
	private static string Base64UrlEncode(byte[] input)
	{
		return Convert.ToBase64String(input)
				.TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}
	private static (string verifier, string challenge) GeneratePkce()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		var verifier = Base64UrlEncode(bytes);
		var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
		var challenge = Base64UrlEncode(hash);
		return (verifier, challenge);
	}
	#endregion

	#region Auth URL builder
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
}
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Templates.Core.Authentication.Models;
using Templates.Core.Authentication.Maui.Events;
using Templates.Core.Authentication.Maui.Storage;
using Templates.Core.Authentication.Maui.Responses;

namespace Templates.Core.Authentication.Maui.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Full Keycloak authentication service for .NET MAUI Blazor Hybrid.
///              Implements Authorization Code + PKCE using <see cref="WebAuthenticator"/> and provides:
///              - Login (browser redirect → code exchange)
///              - Silent refresh via refresh_token grant
///              - Secure token persistence in OS secure storage
///              - Logout (revoke + end-session)
///              - Claims extraction from the access token
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
			#region Generate PKCE pair
			var (codeVerifier, codeChallenge) = GeneratePkce();
			#endregion

			#region Build the authorization URL
			var state = GenerateState();
			var redirectUri = $"{_options.ClientId}://callback";

			var authUrl = new Uri(BuildAuthUrl(codeChallenge, state, redirectUri));
			#endregion

			#region Launch system browser and wait for callback
			var result = await WebAuthenticator.Default.AuthenticateAsync(
				new WebAuthenticatorOptions
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
			#endregion

			#region Exchange authorization code for tokens
			var tokens = await ExchangeCodeAsync(code, codeVerifier, redirectUri, ct);
			await PersistAndNotify(tokens, ct);

			_logger.LogInformation("Keycloak: user {Username} logged in.", _user?.FindFirst("preferred_username")?.Value);
			#endregion

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
			try
			{
				await RevokeTokenAsync(current.RefreshToken, ct);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Keycloak: failed to revoke refresh token (ignored).");
			}
		}

		if (current?.IdToken is not null)
		{
			try
			{
				var endSessionUrl = $"{_options.LogoutUrl}"
					+ $"?id_token_hint={Uri.EscapeDataString(current.IdToken)}"
					+ $"&post_logout_redirect_uri={Uri.EscapeDataString($"{_options.ClientId}://loggedout")}";

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

	#region Get access token (with silent refresh)
	/// <inheritdoc />
	public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
	{
		if (_current is null)
			throw new UnauthorizedAccessException("Not logged in.");

		if (!_current.IsExpired)
			return _current.AccessToken;

		if (!_current.CanRefresh)
			throw new UnauthorizedAccessException("Session expired. Please log in again.");

		return await RefreshSilentlyAsync(ct);
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
				await RefreshSilentlyAsync(ct);
				_logger.LogInformation("Keycloak: session refreshed on restore.");
				return true;
			}

			_logger.LogInformation("Keycloak: stored session expired and has no refresh token.");
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

	#region Internal helpers
	private void NotifyAuthStateChanged(bool isAuthenticated)
	{
		AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(isAuthenticated, _user));
	}

	private async Task<string> RefreshSilentlyAsync(CancellationToken ct)
	{
		await _refreshLock.WaitAsync(ct);
		try
		{
			if (_current is not null && !_current.IsExpired)
				return _current.AccessToken;

			if (_current?.RefreshToken is null)
				throw new UnauthorizedAccessException("No refresh token available.");

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
		_current = tokens;
		_user = ParseUserFromToken(tokens.AccessToken);
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

		var revokeUrl = $"{_options.IssuerUrl}/protocol/openid-connect/revoke";
		await _http.PostAsync(revokeUrl, body, ct);
	}

	private async Task<KeycloakTokenSet> RefreshTokenGrantAsync(string refreshToken, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
			["client_id"] = _options.ClientId
		});

		return await PostToTokenEndpointAsync(body, ct);
	}

	private async Task<KeycloakTokenSet> PostToTokenEndpointAsync(FormUrlEncodedContent body, CancellationToken ct)
	{
		var response = await _http.PostAsync(_options.TokenUrl, body, ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadAsStringAsync(ct);
			throw new InvalidOperationException($"Keycloak token endpoint returned {response.StatusCode}: {error}");
		}

		var json = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct)
			?? throw new InvalidOperationException("Keycloak returned an empty token response.");

		return new KeycloakTokenSet
		{
			IdToken = json.IdToken,
			AccessToken = json.AccessToken,
			RefreshToken = json.RefreshToken,
			ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn)
		};
	}

	private async Task<KeycloakTokenSet> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken ct)
	{
		var body = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["code"] = code,
			["redirect_uri"] = redirectUri,
			["code_verifier"] = codeVerifier,
			["client_id"] = _options.ClientId,
			["grant_type"] = "authorization_code"
		});

		return await PostToTokenEndpointAsync(body, ct);
	}
	#endregion

	#region PKCE helpers
	private static string GenerateState()
	{
		return Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
	}

	private static string Base64UrlEncode(byte[] input)
	{
		return Convert.ToBase64String(input)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
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
	private string BuildAuthUrl(string codeChallenge, string state, string redirectUri)
	{
		var scopes = Uri.EscapeDataString(_options.Scopes);
		return $"{_options.AuthorizationUrl}" +
			   $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
			   $"&response_type=code" +
			   $"&scope={scopes}" +
			   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
			   $"&state={state}" +
			   $"&code_challenge={codeChallenge}" +
			   $"&code_challenge_method=S256";
	}
	#endregion

	#region JWT claims parsing
	private static ClaimsPrincipal ParseUserFromToken(string accessToken)
	{
		try
		{
			var handler = new JwtSecurityTokenHandler();
			if (!handler.CanReadToken(accessToken))
				return new ClaimsPrincipal();

			var jwt = handler.ReadJwtToken(accessToken);
			var claims = new List<Claim>(jwt.Claims);

			// Flatten realm roles
			var realmRolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access");
			if (realmRolesClaim is not null)
			{
				try
				{
					using var doc = JsonDocument.Parse(realmRolesClaim.Value);
					if (doc.RootElement.TryGetProperty("roles", out var roles))
						foreach (var r in roles.EnumerateArray())
							if (r.GetString() is { } rv)
								claims.Add(new Claim(ClaimTypes.Role, rv));
				}
				catch
				{
				}
			}

			var identity = new ClaimsIdentity(claims, "keycloak", "preferred_username", ClaimTypes.Role);
			return new ClaimsPrincipal(identity);
		}
		catch
		{
			return new ClaimsPrincipal();
		}
	}
	#endregion
}
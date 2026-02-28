namespace Pivot.Framework.Authentication.Blazor.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : The payload stored in Redis for a single authenticated Blazor session.
///              Contains the full Keycloak token set plus PKCE/OAuth2 flow state that
///              must survive the browser redirect round-trip to Keycloak.
/// </summary>
public sealed class BlazorTokenSession
{
	#region Token payload (populated after callback)
	public string? IdToken { get; set; }
	public string? AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public DateTimeOffset? ExpiresAt { get; set; }
	public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
	#endregion

	#region Flow state (populated before redirect, cleared after callback)
	/// <summary>
	/// Nonce for ID-token replay protection.
	/// </summary>
	public string? Nonce { get; set; }
	/// <summary>
	/// Where to send the user after a successful login.
	/// </summary>
	public string? ReturnUrl { get; set; }

	/// <summary>
	/// OAuth2 state parameter for CSRF protection.
	/// </summary>
	public string? OAuthState { get; set; }

	/// <summary>
	/// PKCE code verifier stored server-side so it never touches the browser.
	/// </summary>
	public string? PkceVerifier { get; set; }
	#endregion

	#region Helpers
	public bool HasTokens
	{
		get
		{
			return !string.IsNullOrEmpty(AccessToken) && ExpiresAt.HasValue;
		}
	}

	public bool IsExpired
	{
		get
		{
			return !HasTokens || DateTimeOffset.UtcNow >= ExpiresAt!.Value.AddSeconds(-30);
		}
	}

	public bool CanRefresh
	{
		get
		{
			if (string.IsNullOrEmpty(RefreshToken)) return false;
			if (RefreshTokenExpiresAt is null) return true;
			return DateTimeOffset.UtcNow < RefreshTokenExpiresAt.Value.AddSeconds(-30);
		}
	}
	#endregion
}
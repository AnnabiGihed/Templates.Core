namespace Templates.Core.Authentication;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents the OAuth2 token set returned by Keycloak.
///              Immutable — create a new instance on every refresh.
/// </summary>
public sealed record KeycloakTokenSet
{
	#region Properties
	/// <summary>
	/// Returns true when the access token has expired (with 30s buffer).
	/// </summary>
	public bool IsExpired
	{
		get
		{
			return DateTimeOffset.UtcNow >= ExpiresAt.AddSeconds(-30);
		}
	}

	/// <summary>
	/// Returns true when we have a refresh token that has not itself expired.
	/// Uses a 30-second buffer to account for clock skew.
	/// </summary>
	public bool CanRefresh
	{
		get
		{
			if (string.IsNullOrEmpty(RefreshToken))
				return false;

			// If Keycloak didn't give us a refresh expiry (e.g. offline tokens), assume it is still valid.
			if (RefreshTokenExpiresAt is null)
				return true;

			return DateTimeOffset.UtcNow < RefreshTokenExpiresAt.Value.AddSeconds(-30);
		}
	}

	public string? IdToken { get; init; }

	public string? RefreshToken { get; init; }

	public DateTimeOffset ExpiresAt { get; init; }

	public string AccessToken { get; init; } = string.Empty;

	/// <summary>
	/// When the refresh token itself expires. Null when Keycloak doesn't return
	/// refresh_expires_in (e.g. offline_access tokens that never expire server-side).
	/// </summary>
	public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
	#endregion
}
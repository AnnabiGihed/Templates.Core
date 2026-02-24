namespace Templates.Core.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Serializable snapshot of the claims extracted from a valid Keycloak JWT.
///              Stored in Redis keyed by SHA-256(access_token) to avoid re-parsing on
///              every request.
/// </summary>
public sealed class CachedTokenClaims
{
	#region Properties
	public string Email { get; init; } = string.Empty;

	/// <summary>
	/// Keycloak sub claim (UUID string).
	/// Maps to <see cref="Guid"/> via ICurrentUser.UserId.
	/// </summary>
	public string UserId { get; init; } = string.Empty;

	public string Username { get; init; } = string.Empty;

	/// <summary>
	/// Flattened realm + client roles.
	/// </summary>
	public List<string> Roles { get; init; } = [];

	/// <summary>
	/// All raw claims as type → list-of-values to preserve multi-value claims.
	/// </summary>
	public Dictionary<string, List<string>> AllClaims { get; init; } = [];
	#endregion
}
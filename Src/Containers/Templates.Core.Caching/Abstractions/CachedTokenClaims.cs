namespace Templates.Core.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Serializable snapshot of the claims extracted from a valid Keycloak JWT.
///              Stored in Redis keyed by SHA-256(access_token) to avoid re-parsing on
///              every request.
///
/// NOTE ON UserId TYPE:
///   UserId is stored as <c>string</c> here because this is a Redis cache DTO —
///   it must survive JSON round-trips cleanly. The Guid representation lives in
///   <see cref="Templates.Core.Authentication.Models.ICurrentUser.UserId"/>, which
///   parses this string value on access. Keycloak's sub claim is always a UUID, so
///   <c>Guid.TryParse(UserId, out _)</c> will always succeed for valid tokens.
/// </summary>
public sealed class CachedTokenClaims
{
	/// <summary>Keycloak sub claim (UUID string). Maps to <see cref="Guid"/> via ICurrentUser.UserId.</summary>
	public string UserId { get; init; } = string.Empty;

	public string Username { get; init; } = string.Empty;

	public string Email { get; init; } = string.Empty;

	/// <summary>Flattened realm + client roles.</summary>
	public List<string> Roles { get; init; } = [];

	/// <summary>All raw claims as key→value pairs for full fidelity rebuild.</summary>
	public Dictionary<string, string> AllClaims { get; init; } = [];
}
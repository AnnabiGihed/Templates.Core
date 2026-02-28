namespace Pivot.Framework.Authentication.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines a distributed JWT claims cache backed by Redis.
///              Caches validated claim sets for the remaining token lifetime to avoid
///              re-parsing and repeated validation work on subsequent requests.
/// </summary>
public interface IDistributedTokenCache
{
	/// <summary>
	/// Removes a cached entry. Call on revocation or logout.
	/// </summary>
	Task InvalidateAsync(string accessToken, CancellationToken ct = default);

	/// <summary>
	/// Returns cached claims for the given access token, or <c>null</c> if not cached.
	/// Key is derived from a SHA-256 hash of the token — the raw token is never stored.
	/// </summary>
	Task<CachedTokenClaims?> GetClaimsAsync(string accessToken, CancellationToken ct = default);

	/// <summary>
	/// Caches the claims for an access token until it expires.
	/// </summary>
	Task SetClaimsAsync(string accessToken, CachedTokenClaims claims, DateTimeOffset tokenExpiresAt, CancellationToken ct = default);
}
namespace Templates.Core.Caching.Abstractions;

// ─────────────────────────────────────────────────────────────────────────────
// IDistributedTokenCache
// After a JWT is validated once, its parsed claims are cached in Redis for the
// remainder of the token's lifetime. Subsequent requests with the same token
// skip Keycloak metadata fetching and local signature verification.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Caches validated JWT claim sets in Redis to avoid re-parsing on every request.
/// </summary>
public interface IDistributedTokenCache
{
	/// <summary>Removes a cached entry. Call on revocation or logout.</summary>
	Task InvalidateAsync(string accessToken, CancellationToken ct = default);

	/// <summary>
	/// Returns cached claims for the given access token, or <c>null</c> if not cached.
	/// Key is derived from a SHA-256 hash of the token — the raw token is never stored.
	/// </summary>
	Task<CachedTokenClaims?> GetClaimsAsync(string accessToken, CancellationToken ct = default);

	/// <summary>Caches the claims for an access token until it expires.</summary>
	Task SetClaimsAsync(string accessToken, CachedTokenClaims claims, DateTimeOffset tokenExpiresAt, CancellationToken ct = default);
}
namespace Templates.Core.Caching.Abstractions;

// ─────────────────────────────────────────────────────────────────────────────
// ITokenRevocationCache
// When a user logs out, their token(s) are added to a Redis blacklist.
// The JWT bearer handler checks this before accepting a token.
// This enables real-time logout even for tokens with a long remaining lifetime.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Token revocation blacklist backed by Redis.
/// Allows immediate logout even before a JWT expires.
/// </summary>
public interface ITokenRevocationCache
{
	/// <summary>Returns <c>true</c> if the specific token has been explicitly revoked.</summary>
	Task<bool> IsRevokedAsync(string accessToken, CancellationToken ct = default);

	/// <summary>
	/// Marks a single token as revoked.
	/// The Redis entry auto-expires when the token would have expired anyway.
	/// </summary>
	Task RevokeAsync(
		string accessToken,
		DateTimeOffset tokenExpiresAt,
		CancellationToken ct = default);

	/// <summary>
	/// Revokes ALL tokens for a user identified by their Keycloak sub claim as <see cref="Guid"/>.
	/// Stores a "revoke-all-before" timestamp — any token with an <c>iat</c> earlier than
	/// this timestamp is rejected, killing all active sessions across all devices.
	/// </summary>
	Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);

	/// <summary>
	/// Returns <c>true</c> if the token was issued before the user's global revocation timestamp.
	/// </summary>
	Task<bool> IsIssuedBeforeRevocationAsync(
		Guid userId,
		DateTimeOffset tokenIssuedAt,
		CancellationToken ct = default);
}
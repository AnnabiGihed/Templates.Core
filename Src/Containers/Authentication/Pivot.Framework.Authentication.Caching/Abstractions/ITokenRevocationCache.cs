namespace Pivot.Framework.Authentication.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines a token revocation blacklist backed by Redis to allow immediate logout,
///              including per-token revocation and global user revocation based on token issue time.
/// </summary>
public interface ITokenRevocationCache
{
	/// <summary>
	/// Revokes ALL tokens for a user identified by their Keycloak sub claim as <see cref="Guid"/>.
	/// Stores a "revoke-all-before" timestamp — any token with an <c>iat</c> earlier than
	/// this timestamp is rejected, killing all active sessions across all devices.
	/// </summary>
	Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);

	/// <summary>
	/// Returns <c>true</c> if the specific token has been explicitly revoked.
	/// </summary>
	Task<bool> IsRevokedAsync(string accessToken, CancellationToken ct = default);

	/// <summary>
	/// Marks a single token as revoked.
	/// The Redis entry auto-expires when the token would have expired anyway.
	/// </summary>
	Task RevokeAsync(string accessToken, DateTimeOffset tokenExpiresAt, CancellationToken ct = default);

	/// <summary>
	/// Returns <c>true</c> if the token was issued before the user's global revocation timestamp.
	/// </summary>
	Task<bool> IsIssuedBeforeRevocationAsync(Guid userId, DateTimeOffset tokenIssuedAt, CancellationToken ct = default);
}
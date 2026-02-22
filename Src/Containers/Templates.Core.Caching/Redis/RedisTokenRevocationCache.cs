using System.Text;
using System.Security.Cryptography;
using Templates.Core.Caching.Abstractions;

namespace Templates.Core.Caching.Redis;

/// <inheritdoc />
internal sealed class RedisTokenRevocationCache : ITokenRevocationCache
{
	// Redis key patterns:
	//   tkn:revoked:<sha256-of-token>      → sentinel (individual token blacklisted)
	//   tkn:revoke-all:<userId-as-Guid>    → UnixSeconds (global revocation timestamp for user)

	private const string RevokedPrefix = "tkn:revoked:";
	private const string RevokeAllPrefix = "tkn:revoke-all:";

	#region Dependencies
	private readonly ICacheService _cache;
	#endregion

	#region Constructor
	public RedisTokenRevocationCache(ICacheService cache) => _cache = cache;
	#endregion

	// ─── Individual token revocation ─────────────────────────────────────────

	public async Task<bool> IsRevokedAsync(string accessToken, CancellationToken ct = default) =>
		await _cache.ExistsAsync(BuildRevokedKey(accessToken), ct);

	public async Task RevokeAsync(
		string accessToken,
		DateTimeOffset tokenExpiresAt,
		CancellationToken ct = default)
	{
		var ttl = tokenExpiresAt - DateTimeOffset.UtcNow;
		if (ttl <= TimeSpan.Zero) return; // already expired — no point storing

		await _cache.SetAsync(BuildRevokedKey(accessToken), new RevokedSentinel(), ttl, ct);

		// Also evict from token claims cache so no stale claims are served
		await _cache.RemoveAsync(BuildClaimsKey(accessToken), ct);
	}

	// ─── Global user revocation ──────────────────────────────────────────────

	/// <inheritdoc />
	/// <remarks>
	/// Uses the Keycloak sub GUID as the Redis key segment.
	/// Consistent with <see cref="ICurrentUser.UserId"/> which is typed as <see cref="Guid"/>.
	/// </remarks>
	public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
	{
		var sentinel = new RevokeAllSentinel
		{
			RevokedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
		};

		// Keep for 90 days — exceeds any realistic refresh token lifetime
		await _cache.SetAsync(
			BuildRevokeAllKey(userId),
			sentinel,
			TimeSpan.FromDays(90),
			ct);
	}

	/// <inheritdoc />
	public async Task<bool> IsIssuedBeforeRevocationAsync(
		Guid userId,
		DateTimeOffset tokenIssuedAt,
		CancellationToken ct = default)
	{
		var sentinel = await _cache.GetAsync<RevokeAllSentinel>(BuildRevokeAllKey(userId), ct);
		if (sentinel is null) return false;

		return tokenIssuedAt.ToUnixTimeSeconds() < sentinel.RevokedAtUnix;
	}

	// ─── Key builders ─────────────────────────────────────────────────────────

	private static string BuildRevokedKey(string accessToken) =>
		$"{RevokedPrefix}{HashToken(accessToken)}";

	// Same SHA-256 scheme as RedisDistributedTokenCache so keys stay consistent
	private static string BuildClaimsKey(string accessToken) =>
		$"tkn:claims:{HashToken(accessToken)}";

	/// <summary>
	/// Uses the Guid's canonical lowercase string form ("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")
	/// as the key segment — deterministic, compact, and unique.
	/// </summary>
	private static string BuildRevokeAllKey(Guid userId) =>
		$"{RevokeAllPrefix}{userId:D}";

	private static string HashToken(string token) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

	// ─── Internal DTOs ────────────────────────────────────────────────────────

	private sealed class RevokedSentinel { public string V { get; init; } = "1"; }

	private sealed class RevokeAllSentinel
	{
		public long RevokedAtUnix { get; init; }
	}
}
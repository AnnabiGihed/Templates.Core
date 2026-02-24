using System.Text;
using System.Security.Cryptography;
using Templates.Core.Caching.Abstractions;

namespace Templates.Core.Caching.Redis;

/// <inheritdoc />
internal sealed class RedisTokenRevocationCache : ITokenRevocationCache
{
	#region Constants
	private const string ClaimsPrefix = "tkn:claims:";
	private const string RevokedPrefix = "tkn:revoked:";
	private const string RevokeAllPrefix = "tkn:revoke-all:";
	#endregion

	#region Dependencies
	private readonly ICacheService _cache;
	/// <summary>
	/// How long a "revoke-all-for-user" sentinel is kept in Redis.
	/// Must be at least as long as Keycloak's maximum refresh token lifetime.
	/// Configurable via <see cref="TokenRevocationOptions.RevokeAllTtl"/>.
	/// </summary>
	private readonly TimeSpan _revokeAllTtl;
	#endregion

	#region Constructor
	public RedisTokenRevocationCache(ICacheService cache, TokenRevocationOptions options)
	{
		_cache = cache;
		_revokeAllTtl = options.RevokeAllTtl;
	}
	#endregion

	#region Individual token revocation
	public async Task<bool> IsRevokedAsync(string accessToken, CancellationToken ct = default)
	{
		return await _cache.ExistsAsync(BuildRevokedKey(accessToken), ct);
	}
	public async Task RevokeAsync(string accessToken, DateTimeOffset tokenExpiresAt, CancellationToken ct = default)
	{
		var ttl = tokenExpiresAt - DateTimeOffset.UtcNow;
		if (ttl <= TimeSpan.Zero) return;

		await _cache.SetAsync(BuildRevokedKey(accessToken), new RevokedSentinel(), ttl, ct);
		await _cache.RemoveAsync(BuildClaimsKey(accessToken), ct);
	}
	#endregion

	#region Global user revocation
	/// <inheritdoc />
	public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
	{
		var sentinel = new RevokeAllSentinel
		{
			RevokedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
		};

		await _cache.SetAsync(BuildRevokeAllKey(userId), sentinel, _revokeAllTtl, ct);
	}

	/// <inheritdoc />
	public async Task<bool> IsIssuedBeforeRevocationAsync(Guid userId, DateTimeOffset tokenIssuedAt, CancellationToken ct = default)
	{
		var sentinel = await _cache.GetAsync<RevokeAllSentinel>(
			BuildRevokeAllKey(userId), ct);

		if (sentinel is null) return false;

		return tokenIssuedAt.ToUnixTimeSeconds() < sentinel.RevokedAtUnix;
	}
	#endregion

	#region Key builders
	private static string HashToken(string token)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
	}
	private static string BuildRevokeAllKey(Guid userId)
	{
		return $"{RevokeAllPrefix}{userId:D}";
	}
	private static string BuildClaimsKey(string accessToken)
	{
		return $"{ClaimsPrefix}{HashToken(accessToken)}";
	}
	private static string BuildRevokedKey(string accessToken)
	{
		return $"{RevokedPrefix}{HashToken(accessToken)}";
	}
	#endregion

	#region Internal DTOs
	private sealed class RevokedSentinel { public string V { get; init; } = "1"; }
	private sealed class RevokeAllSentinel { public long RevokedAtUnix { get; init; } }
	#endregion
}
using System.Text;
using System.Security.Cryptography;
using Pivot.Framework.Infrastructure.Caching.Abstractions;
using Pivot.Framework.Authentication.Caching.Abstractions;

namespace Pivot.Framework.Authentication.Caching.Redis;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Redis-backed implementation of <see cref="ITokenRevocationCache"/>.
///              Supports per-token blacklisting and global user revocation via a
///              "revoke-all-before" timestamp sentinel stored in Redis.
///              Delegates all Redis I/O to <see cref="ICacheService"/>.
/// </summary>
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
		ArgumentNullException.ThrowIfNull(cache);
		ArgumentNullException.ThrowIfNull(options);
		_cache = cache;
		_revokeAllTtl = options.RevokeAllTtl;
	}
	#endregion

	#region Individual Token Revocation
	/// <inheritdoc />
	public async Task<bool> IsRevokedAsync(string accessToken, CancellationToken ct = default)
	{
		return await _cache.ExistsAsync(BuildRevokedKey(accessToken), ct);
	}

	/// <inheritdoc />
	public async Task RevokeAsync(string accessToken, DateTimeOffset tokenExpiresAt, CancellationToken ct = default)
	{
		var ttl = tokenExpiresAt - DateTimeOffset.UtcNow;
		if (ttl <= TimeSpan.Zero)
			return;

		await _cache.SetAsync(BuildRevokedKey(accessToken), new RevokedSentinel(), ttl, ct);
		await _cache.RemoveAsync(BuildClaimsKey(accessToken), ct);
	}
	#endregion

	#region Global User Revocation
	/// <inheritdoc />
	public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
	{
		var sentinel = new RevokeAllSentinel { RevokedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
		await _cache.SetAsync(BuildRevokeAllKey(userId), sentinel, _revokeAllTtl, ct);
	}

	/// <inheritdoc />
	public async Task<bool> IsIssuedBeforeRevocationAsync(Guid userId, DateTimeOffset tokenIssuedAt, CancellationToken ct = default)
	{
		var sentinel = await _cache.GetAsync<RevokeAllSentinel>(BuildRevokeAllKey(userId), ct);
		if (sentinel is null)
			return false;

		return tokenIssuedAt.ToUnixTimeSeconds() < sentinel.RevokedAtUnix;
	}
	#endregion

	#region Key Builders
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
	private sealed class RevokedSentinel
	{
		/// <summary>Marker value — presence of the key is what matters, not its content.</summary>
		public string V { get; init; } = "1";
	}

	private sealed class RevokeAllSentinel
	{
		/// <summary>Unix timestamp of the revocation event.</summary>
		public long RevokedAtUnix { get; init; }
	}
	#endregion
}
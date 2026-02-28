using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Pivot.Framework.Authentication.Blazor.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Redis-backed server-side token session store for Blazor.
///              Tokens are stored entirely on the server — the browser only ever receives
///              an opaque session cookie. TTL is derived from the token's actual expiry
///              so sessions auto-evict without a background job.
/// </summary>
internal sealed class RedisBlazorTokenSessionStore : IBlazorTokenSessionStore
{
	#region Constants
	private const string Prefix = "blazor:session:";

	/// <summary>
	/// Minimum TTL for an incomplete login-flow session (PKCE state before callback).
	/// After this the user must start over.
	/// </summary>
	private static readonly TimeSpan FlowStateTtl = TimeSpan.FromMinutes(10);
	#endregion

	#region Dependencies
	private readonly IDistributedCache _cache;
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
	};
	#endregion

	#region Constructor
	public RedisBlazorTokenSessionStore(IDistributedCache cache)
	{
		_cache = cache;
	}
	#endregion

	#region IBlazorTokenSessionStore
	public Task RemoveAsync(string sessionId, CancellationToken ct = default)
	{
		return _cache.RemoveAsync(BuildKey(sessionId), ct);
	}
	public async Task<BlazorTokenSession?> GetAsync(string sessionId, CancellationToken ct = default)
	{
		var bytes = await _cache.GetAsync(BuildKey(sessionId), ct);

		if (bytes is null || bytes.Length == 0)
			return null;

		return JsonSerializer.Deserialize<BlazorTokenSession>(bytes, JsonOpts);
	}
	public async Task SaveAsync(string sessionId, BlazorTokenSession session, CancellationToken ct = default)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(session, JsonOpts);

		var ttl = session.HasTokens && session.RefreshTokenExpiresAt.HasValue
			? session.RefreshTokenExpiresAt.Value - DateTimeOffset.UtcNow
			: session.HasTokens && session.ExpiresAt.HasValue
				? session.ExpiresAt.Value - DateTimeOffset.UtcNow + TimeSpan.FromHours(1)
				: FlowStateTtl;

		if (ttl <= TimeSpan.Zero)
			ttl = FlowStateTtl;

		await _cache.SetAsync(BuildKey(sessionId), bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
	}
	#endregion

	#region Private helpers
	private static string BuildKey(string sessionId) => $"{Prefix}{sessionId}";
	#endregion
}
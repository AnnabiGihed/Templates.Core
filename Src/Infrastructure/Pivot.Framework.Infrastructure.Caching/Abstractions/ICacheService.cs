namespace Pivot.Framework.Infrastructure.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines a generic typed distributed cache abstraction over
///              <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> using JSON serialization,
///              so callers never manipulate raw byte arrays.
///              Registered via <see cref="Extensions.RedisCachingExtensions.AddRedisCache"/>.
/// </summary>
public interface ICacheService
{
	/// <summary>
	/// Removes a key (no-op if absent).
	/// </summary>
	Task RemoveAsync(string key, CancellationToken ct = default);

	/// <summary>
	/// Returns <c>true</c> if the key exists.
	/// </summary>
	Task<bool> ExistsAsync(string key, CancellationToken ct = default);

	/// <summary>
	/// Gets a cached value.
	/// Returns <c>null</c> if the key does not exist.
	/// </summary>
	Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

	/// <summary>
	/// Sets a value with an absolute expiry.
	/// </summary>
	Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, CancellationToken ct = default)
		where T : class;
}
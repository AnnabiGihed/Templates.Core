using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Caching.Redis;
using Pivot.Framework.Infrastructure.Caching.Abstractions;

namespace Pivot.Framework.Infrastructure.Caching.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Extension methods to register the generic Redis caching infrastructure into the DI container.
///              No authentication, token, or JWT concerns belong here.
///              For JWT token caching and revocation, see Pivot.Framework.Authentication.Caching.
/// </summary>
public static class RedisCachingExtensions
{
	#region Public Methods
	/// <summary>
	/// Registers the Redis-backed <see cref="ICacheService"/> into the DI container.
	/// Reads the connection string from <c>ConnectionStrings:Redis</c> in appsettings.json
	/// unless <paramref name="connectionString"/> is provided explicitly.
	/// </summary>
	public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration, string? connectionString = null, string instanceName = "TemplatesCore:")
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		var connStr = connectionString
			?? configuration.GetConnectionString("Redis")
			?? throw new InvalidOperationException("Missing Redis connection string. Add 'ConnectionStrings:Redis' to appsettings.json.");

		services.AddStackExchangeRedisCache(opt =>
		{
			opt.Configuration = connStr;
			opt.InstanceName = instanceName;
		});

		services.AddSingleton<ICacheService, RedisCacheService>();

		return services;
	}
	#endregion
}
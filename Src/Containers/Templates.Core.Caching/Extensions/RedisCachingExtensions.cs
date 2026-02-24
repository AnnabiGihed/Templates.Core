using Microsoft.Extensions.Options;
using Templates.Core.Caching.Redis;
using Templates.Core.Authentication;
using Templates.Core.Caching.Handlers;
using Microsoft.Extensions.Configuration;
using Templates.Core.Caching.Abstractions;
using Templates.Core.Authentication.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Templates.Core.Caching.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Extension methods to add Redis-backed token caching and revocation to a backend API.
/// </summary>
public static class RedisCachingExtensions
{
	#region Public Methods
	/// <summary>
	/// Extracts the raw Bearer token from the current request's Authorization header.
	/// Call this in your logout endpoint to revoke the calling user's own token.
	/// </summary>
	public static string? GetBearerToken(this Microsoft.AspNetCore.Http.HttpContext ctx)
	{
		var raw = ctx.Request.Headers.Authorization.ToString();
		if (string.IsNullOrWhiteSpace(raw)) 
			return null;

		var token = raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? raw["Bearer ".Length..].Trim() : raw.Trim();

		return string.IsNullOrEmpty(token) ? null : token;
	}

	/// <summary>
	/// Registers the full Redis caching stack:
	/// - <see cref="ICacheService"/>           (generic typed cache)
	/// - <see cref="IDistributedTokenCache"/>  (JWT claims cache)
	/// - <see cref="ITokenRevocationCache"/>   (logout blacklist)
	/// - <see cref="TokenRevocationOptions"/>  (configurable TTLs)
	/// </summary>
	public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration, string? connectionString = null, string instanceName = "TemplatesCore:")
	{
		var connStr = connectionString?? configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Missing Redis connection string. " + "Add 'ConnectionStrings:Redis' to appsettings.json.");

		services.AddStackExchangeRedisCache(opt =>
		{
			opt.Configuration = connStr;
			opt.InstanceName = instanceName;
		});

		// Bind revocation TTL options (falls back to 30-day default if not configured)
		services.Configure<TokenRevocationOptions>(configuration.GetSection(TokenRevocationOptions.SectionName));

		services.AddSingleton<ICacheService, RedisCacheService>();
		services.AddSingleton<IDistributedTokenCache, RedisDistributedTokenCache>();
		services.AddSingleton<ITokenRevocationCache>(sp => new RedisTokenRevocationCache(sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<IOptions<TokenRevocationOptions>>().Value));

		return services;
	}

	/// <summary>
	/// Composite one-liner that registers:
	/// 1. Keycloak JWT authentication
	/// 2. Redis caching + revocation
	/// 3. <see cref="KeycloakRedisJwtEvents"/> wired into the JWT bearer pipeline
	/// </summary>
	public static IServiceCollection AddKeycloakRedisCache(this IServiceCollection services, IConfiguration configuration, string? redisConnectionString = null, string redisInstanceName = "TemplatesCore:")
	{
		#region Redis services
		services.AddRedisCache(configuration, redisConnectionString, redisInstanceName);
		#endregion

		#region Redis JWT events
		services.AddSingleton<KeycloakRedisJwtEvents>();
		#endregion

		#region Keycloak authentication
		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUser, CurrentUser>();

		services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
			{
				var keycloak = configuration.GetSection(KeycloakOptions.SectionName)
					.Get<KeycloakOptions>()!;

				keycloak.Validate();

				jwt.Audience = keycloak.Audience;
				jwt.Authority = keycloak.IssuerUrl;
				jwt.MetadataAddress = keycloak.MetadataUrl;
				jwt.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;

				jwt.TokenValidationParameters =
					new Microsoft.IdentityModel.Tokens.TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateLifetime = true,
						ValidateAudience = true,
						RoleClaimType = "roles",
						ValidIssuer = keycloak.IssuerUrl,
						ValidAudience = keycloak.Audience,
						ClockSkew = TimeSpan.FromSeconds(30),
						NameClaimType = "preferred_username"
					};

				jwt.EventsType = typeof(KeycloakRedisJwtEvents);
			});

		services.AddAuthorization();
		#endregion

		return services;
	}
	#endregion
}
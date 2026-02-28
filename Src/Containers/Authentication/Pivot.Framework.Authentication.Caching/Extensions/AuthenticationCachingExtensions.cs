using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Pivot.Framework.Authentication.Models;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Authentication.Caching.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Pivot.Framework.Authentication.Caching.Handlers;
using Pivot.Framework.Infrastructure.Caching.Extensions;
using Pivot.Framework.Authentication.Caching.Abstractions;
using Pivot.Framework.Infrastructure.Caching.Abstractions;

namespace Pivot.Framework.Authentication.Caching.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Extension methods that compose Redis caching infrastructure with Keycloak JWT
///              authentication domain services into a single registration call for ASP.NET Core APIs.
/// </summary>
public static class AuthenticationCachingExtensions
{
	#region Public Methods
	/// <summary>
	/// Composite one-liner that registers:
	/// <list type="bullet">
	///   <item>Redis <see cref="ICacheService"/> via <see cref="RedisCachingExtensions.AddRedisCache"/></item>
	///   <item><see cref="IDistributedTokenCache"/> and <see cref="ITokenRevocationCache"/> with their Redis implementations</item>
	///   <item><see cref="KeycloakRedisJwtEvents"/> wired into the JWT bearer pipeline</item>
	///   <item>Keycloak JWT bearer authentication and authorization</item>
	///   <item><see cref="ICurrentUser"/> / <see cref="CurrentUser"/> scoped service</item>
	/// </list>
	/// </summary>
	public static IServiceCollection AddKeycloakAuthenticationCaching(this IServiceCollection services, IConfiguration configuration, string? redisConnectionString = null, string redisInstanceName = "TemplatesCore:")
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		#region Redis Infrastructure
		services.AddRedisCache(configuration, redisConnectionString, redisInstanceName);
		#endregion

		#region Authentication Domain Caching Services
		services.Configure<TokenRevocationOptions>(configuration.GetSection(TokenRevocationOptions.SectionName));

		services.AddSingleton<IDistributedTokenCache, RedisDistributedTokenCache>();
		services.AddSingleton<ITokenRevocationCache>(sp =>
		{
			var opts = sp.GetRequiredService<IOptions<TokenRevocationOptions>>().Value;
			opts.Validate();
			return new RedisTokenRevocationCache(sp.GetRequiredService<ICacheService>(), opts);
		});

		services.AddSingleton<KeycloakRedisJwtEvents>();
		#endregion

		#region Keycloak Authentication
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

				jwt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateLifetime = true,
					ValidateAudience = true,
					ValidIssuer = keycloak.IssuerUrl,
					ValidAudience = keycloak.Audience,
					ClockSkew = TimeSpan.FromSeconds(30),
					NameClaimType = "preferred_username",
					RoleClaimType = ClaimTypes.Role
				};

				jwt.EventsType = typeof(KeycloakRedisJwtEvents);
			});

		services.AddAuthorization();
		#endregion

		return services;
	}
	#endregion
}
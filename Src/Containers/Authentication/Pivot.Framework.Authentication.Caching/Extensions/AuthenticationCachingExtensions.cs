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
/// Purpose     : INTERNAL — Redis caching infrastructure + Keycloak JWT registration.
///              Do not call from application code.
///              Public surface is <see cref="KeycloakAuthenticationCachingOptionsExtensions.WithRedisTokenCaching"/>.
/// </summary>
internal static class AuthenticationCachingExtensions
{
	/// <summary>
	/// Composite registration that wires:
	/// <list type="bullet">
	///   <item>Redis <see cref="ICacheService"/></item>
	///   <item><see cref="IDistributedTokenCache"/> and <see cref="ITokenRevocationCache"/></item>
	///   <item><see cref="KeycloakRedisJwtEvents"/> wired into the JWT bearer pipeline</item>
	///   <item>Keycloak JWT bearer authentication and authorization</item>
	///   <item><see cref="ICurrentUser"/> / <see cref="CurrentUser"/> scoped service</item>
	/// </list>
	/// </summary>
	internal static IServiceCollection AddKeycloakAuthenticationCaching(
		this IServiceCollection services,
		IConfiguration configuration,
		string? redisConnectionString = null,
		string redisInstanceName = "TemplatesCore:")
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

		#region Keycloak Authentication — wire events only (JWT bearer is registered by RegisterCoreJwtBearer)
		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUser, CurrentUser>();

		services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

		services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt =>
		{
			jwt.EventsType = typeof(KeycloakRedisJwtEvents);
		});

		services.AddAuthorization();
		#endregion

		return services;
	}
}
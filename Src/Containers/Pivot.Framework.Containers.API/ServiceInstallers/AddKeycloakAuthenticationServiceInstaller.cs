using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Authentication.Caching.Extensions;
using Pivot.Framework.Authentication.AspNetCore.Extensions;

namespace Pivot.Framework.Containers.API.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Wires Keycloak JWT + Redis token caching + Swagger OAuth into DI.
///              Accepts the Swagger document title and version as parameters so this
///              installer remains application-agnostic.
/// </summary>
public static class AddKeycloakAuthenticationServiceInstaller
{
	#region Public Methods
	/// <summary>
	/// Registers Keycloak authentication support, including:
	/// - Redis caching stack (ICacheService, IDistributedTokenCache, ITokenRevocationCache)
	/// - KeycloakRedisJwtEvents wired into the JwtBearer pipeline
	/// - ICurrentUser scoped service
	/// - Swagger/OpenAPI configuration with Keycloak security definition and requirement
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration.</param>
	/// <param name="swaggerTitle">The Swagger document title displayed in the UI (e.g. "My API").</param>
	/// <param name="swaggerVersion">The Swagger document version (e.g. "v1").</param>
	public static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration, string swaggerTitle, string swaggerVersion = "v1")
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentException.ThrowIfNullOrWhiteSpace(swaggerTitle);
		ArgumentException.ThrowIfNullOrWhiteSpace(swaggerVersion);

		services.AddKeycloakAuthenticationCaching(configuration);

		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc(swaggerVersion, new() { Title = swaggerTitle, Version = swaggerVersion });

			// Pass IConfiguration directly to avoid calling services.BuildServiceProvider(),
			// which creates a second DI container root and causes an ASP.NET Core warning.
			c.AddKeycloakSecurityDefinition(configuration);
			c.AddKeycloakSecurityRequirement();
		});
	}
	#endregion
}
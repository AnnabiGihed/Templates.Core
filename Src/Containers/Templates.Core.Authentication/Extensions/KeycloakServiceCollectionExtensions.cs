using Microsoft.Extensions.Configuration;
using Templates.Core.Authentication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Templates.Core.Authentication.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Convenience extensions that compose all backend Keycloak registrations.
///              Registers Keycloak JWT authentication, current user resolution,
///              and HTTP context access.
/// </summary>
public static class KeycloakServiceCollectionExtensions
{
	#region Public Methods
	/// <summary>
	/// Registers:
	/// - Keycloak JWT authentication (via <see cref="KeycloakAuthenticationExtensions"/>)
	/// - <see cref="ICurrentUser"/> scoped service
	/// - <see cref="IHttpContextAccessor"/>
	/// </summary>
	public static IServiceCollection AddKeycloakBackend(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUser, CurrentUser>();
		services.AddKeycloakAuthentication(configuration);

		return services;
	}
	#endregion
}
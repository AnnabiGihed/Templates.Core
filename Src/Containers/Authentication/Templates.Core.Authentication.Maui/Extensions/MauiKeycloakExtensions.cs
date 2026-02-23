using Microsoft.Extensions.Configuration;
using Templates.Core.Authentication.Storage;
using Templates.Core.Authentication.Services;
using Templates.Core.Authentication.Handlers;
using Templates.Core.Authentication.Maui.Storage;
using Templates.Core.Authentication.Maui.Services;

namespace Templates.Core.Authentication.Maui.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Provides extension methods to wire Keycloak authentication into a MAUI application,
///              including DI registrations and HttpClient handler integration.
/// </summary>
public static class MauiKeycloakExtensions
{
	#region Public Methods
	/// <summary>
	/// Registers all Keycloak services for a MAUI Blazor Hybrid app:
	/// - <see cref="IKeycloakAuthService"/>  (singleton)
	/// - <see cref="IKeycloakTokenStorage"/> (singleton)
	/// - <see cref="KeycloakAuthorizationMessageHandler"/> (transient)
	/// - Named HttpClient used internally by the auth service
	/// </summary>
	public static IServiceCollection AddKeycloakMaui(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

		services.AddHttpClient(nameof(KeycloakAuthService));

		services.AddSingleton<IKeycloakTokenStorage, KeycloakTokenStorage>();
		services.AddSingleton<IKeycloakAuthService, KeycloakAuthService>();
		services.AddTransient<KeycloakAuthorizationMessageHandler>();

		return services;
	}

	/// <summary>
	/// Adds the <see cref="KeycloakAuthorizationMessageHandler"/> to an <see cref="IHttpClientBuilder"/>.
	///
	/// Usage:
	/// <code>
	///   services.AddHttpClient("CurviaApi", c => c.BaseAddress = new Uri("https://api.curvia.app"))
	///           .AddKeycloakHandler();
	/// </code>
	/// </summary>
	public static IHttpClientBuilder AddKeycloakHandler(this IHttpClientBuilder builder)
	{
		return builder.AddHttpMessageHandler<KeycloakAuthorizationMessageHandler>();
	}
	#endregion
}
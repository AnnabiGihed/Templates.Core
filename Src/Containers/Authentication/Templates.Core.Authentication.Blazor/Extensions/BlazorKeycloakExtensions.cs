using Microsoft.Extensions.Configuration;
using Templates.Core.Authentication.Services;
using Templates.Core.Authentication.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Authentication.Blazor.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using Templates.Core.Authentication.Blazor.Services;
using Templates.Core.Authentication.Maui.AuthenticationStateProvider;

namespace Templates.Core.Authentication.Blazor.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Wires Keycloak authentication into a Blazor Server application.
/// </summary>
public static class BlazorKeycloakExtensions
{
	#region Public Methods
	/// <summary>
	/// Adds the <see cref="KeycloakAuthorizationMessageHandler"/> to an <see cref="IHttpClientBuilder"/>.
	/// </summary>
	public static IHttpClientBuilder AddKeycloakHandler(this IHttpClientBuilder builder)
	{
		return builder.AddHttpMessageHandler<KeycloakAuthorizationMessageHandler>();
	}

	/// <summary>
	/// Registers all Keycloak services for a Blazor Server application:
	/// <list type="bullet">
	///   <item><see cref="IBlazorTokenSessionStore"/> — Redis-backed server-side token storage.</item>
	///   <item><see cref="IBlazorKeycloakAuthService"/> / <see cref="IKeycloakAuthService"/> — redirect + PKCE auth service.</item>
	///   <item><see cref="AuthenticationStateProvider"/> — Blazor auth state wired to the Keycloak session.</item>
	///   <item><see cref="KeycloakAuthorizationMessageHandler"/> — attaches Bearer tokens to HttpClients.</item>
	/// </list>
	/// </summary>
	/// <param name="services">The DI container.</param>
	/// <param name="configuration">Application configuration (must contain a "Keycloak" section).</param>
	public static IServiceCollection AddKeycloakBlazor(this IServiceCollection services,IConfiguration configuration)
	{
		services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

		// IHttpContextAccessor needed by KeycloakAuthService to read/write the session cookie
		services.AddHttpContextAccessor();

		// Named HttpClient used internally for token endpoint + JWKS calls
		services.AddHttpClient(nameof(KeycloakAuthService));

		// Redis-backed server-side token session store
		// Note: IDistributedCache (Redis) must already be registered
		services.AddScoped<IBlazorTokenSessionStore, RedisBlazorTokenSessionStore>();

		// Keycloak auth service — scoped because NavigationManager and
		// IHttpContextAccessor are both scoped in Blazor Server
		services.AddScoped<KeycloakAuthService>();
		services.AddScoped<IBlazorKeycloakAuthService>(sp => sp.GetRequiredService<KeycloakAuthService>());
		services.AddScoped<IKeycloakAuthService>(sp => sp.GetRequiredService<KeycloakAuthService>());

		// Blazor AuthenticationStateProvider backed by the Keycloak session
		services.AddScoped<AuthenticationStateProvider, KeycloakAuthStateProvider>();

		// HTTP handler that attaches Bearer token to outgoing HttpClients
		services.AddTransient<KeycloakAuthorizationMessageHandler>();

		// Required by <AuthorizeView> and [Authorize]
		services.AddAuthorizationCore();

		return services;
	}
	#endregion
}
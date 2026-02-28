using Microsoft.Extensions.Configuration;
using Pivot.Framework.Authentication.Storage;
using Pivot.Framework.Authentication.Services;
using Pivot.Framework.Authentication.Handlers;
using Pivot.Framework.Authentication.Maui.Storage;
using Pivot.Framework.Authentication.Maui.Services;

namespace Pivot.Framework.Authentication.Maui.Extensions;

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
	///
	/// <para><strong>Platform setup required:</strong> The PKCE callback and post-logout redirect
	/// URIs use a custom scheme in the form <c>{ClientId}://callback</c> and
	/// <c>{ClientId}://loggedout</c>. You must register this scheme on each target platform:</para>
	/// <list type="bullet">
	///   <item>
	///     <term>Android</term>
	///     <description>
	///       Add an <c>IntentFilter</c> with <c>android:scheme="{ClientId}"</c> to your
	///       <c>MainActivity</c> and call <c>WebAuthenticatorCallbackActivity</c>, or use the
	///       <c>[QueryProperty]</c> / <c>[Activity(LaunchMode = LaunchMode.SingleTop)]</c> approach
	///       documented at
	///       <see href="https://learn.microsoft.com/dotnet/maui/platform-integration/communication/authentication"/>.
	///     </description>
	///   </item>
	///   <item>
	///     <term>iOS / macOS</term>
	///     <description>
	///       Add the scheme to the <c>CFBundleURLSchemes</c> array in <c>Info.plist</c>
	///       and ensure <c>OpenUrl</c> / <c>ContinueUserActivity</c> is forwarded to
	///       <c>WebAuthenticator.Default.OpenUrl(url)</c>.
	///     </description>
	///   </item>
	///   <item>
	///     <term>Windows</term>
	///     <description>
	///       Register the protocol in <c>Package.appxmanifest</c> under
	///       <c>Extensions &gt; Protocol</c> with the same scheme name.
	///     </description>
	///   </item>
	/// </list>
	/// <para>Omitting platform registration causes the post-logout redirect and the
	/// OAuth2 callback to fail silently — the browser will not return control to the app.</para>
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
	/// </summary>
	public static IHttpClientBuilder AddKeycloakHandler(this IHttpClientBuilder builder)
	{
		return builder.AddHttpMessageHandler<KeycloakAuthorizationMessageHandler>();
	}
	#endregion
}
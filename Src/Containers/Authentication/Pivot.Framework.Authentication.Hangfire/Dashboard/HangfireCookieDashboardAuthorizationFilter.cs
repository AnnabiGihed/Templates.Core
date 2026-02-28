using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Pivot.Framework.Authentication.Hangfire.Constants;

namespace Pivot.Framework.Authentication.Hangfire.Dashboard;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Hangfire IDashboardAuthorizationFilter that gates the /hangfire dashboard
///              using a dedicated cookie scheme backed by Keycloak OIDC.
///
///              On each request to the dashboard:
///              - If HangfireCookie is present and valid → access granted.
///              - If not authenticated → ChallengeAsync("HangfireOidc") redirects the
///                browser to Keycloak. After login, Keycloak posts back to /hangfire-callback,
///                the OIDC middleware sets the HangfireCookie, and the browser lands on
///                /hangfire fully authenticated.
///
///              Used automatically by UseHangfireDashboardWithKeycloakAuth().
///              Do not combine with HangfireDashboardAuthorizationFilter (JWT-only filter)
///              from Pivot.Framework.Infrastructure.Scheduling — pick one per dashboard mount.
/// </summary>
public sealed class HangfireCookieDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	/// <inheritdoc />
	public bool Authorize(DashboardContext context)
	{
		var httpContext = context.GetHttpContext();

		var result = httpContext
			.AuthenticateAsync(HangfireAuthConstants.CookieScheme)
			.GetAwaiter()
			.GetResult();

		if (result.Succeeded && result.Principal?.Identity?.IsAuthenticated == true)
			return true;

		// Write the redirect and short-circuit the response completely.
		// We must end the response here — returning false hands control back
		// to Hangfire which will overwrite our redirect with a 401.
		httpContext.Response.Redirect("/hangfire-login");
		httpContext.Response.CompleteAsync().GetAwaiter().GetResult();

		// Return true so Hangfire does not write its own 401 over our redirect.
		return true;
	}
}
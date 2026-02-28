using Hangfire.Dashboard;

namespace Pivot.Framework.Infrastructure.Scheduling.Dashboard;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Hangfire <see cref="IDashboardAuthorizationFilter"/> that gates access to the
///              Hangfire dashboard using the standard ASP.NET Core authentication pipeline.
///
///              Aligns with the framework's Keycloak JWT bearer setup
///              (<c>Pivot.Framework.Authentication.AspNetCore</c>): a request is considered
///              authorised when <c>HttpContext.User.Identity.IsAuthenticated</c> is true,
///              which is the same check performed by <c>ICurrentUser.IsAuthenticated</c>
///              and by any <c>[Authorize]</c> controller attribute.
///
///              Optionally, a required role can be provided at construction time so that
///              access can be further restricted to a specific Keycloak realm or client role
///              (e.g. <c>"admin"</c>). Role values are compared against the flattened
///              <c>ClaimTypes.Role</c> claims produced by <see cref="KeycloakClaimsTransformer"/>.
///
/// Usage (require authentication only — default):
/// <code>
/// app.UseHangfireDashboardWithOptions();
/// </code>
///
/// Usage (require a specific Keycloak role):
/// <code>
/// app.UseHangfireDashboardWithOptions(opts =>
/// {
///     opts.Authorization = new[]
///     {
///         new HangfireDashboardAuthorizationFilter(requiredRole: "admin")
///     };
/// });
/// </code>
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	#region Fields
	private readonly string? _requiredRole;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the filter.
	/// </summary>
	/// <param name="requiredRole">
	/// Optional Keycloak role the authenticated user must possess.
	/// When <c>null</c> (default), any authenticated user is granted access.
	/// </param>
	public HangfireDashboardAuthorizationFilter(string? requiredRole = null)
	{
		_requiredRole = requiredRole;
	}
	#endregion

	#region IDashboardAuthorizationFilter
	/// <inheritdoc />
	public bool Authorize(DashboardContext context)
	{
		var httpContext = context.GetHttpContext();
		var user = httpContext.User;

		// Gate 1 — must be authenticated (Keycloak JWT bearer validated by ASP.NET Core middleware).
		if (user.Identity?.IsAuthenticated != true)
			return false;

		// Gate 2 — optional role check against flattened ClaimTypes.Role claims.
		if (_requiredRole is not null)
			return user.IsInRole(_requiredRole);

		return true;
	}
	#endregion
}
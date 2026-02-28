using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Scheduling.Services;
using Pivot.Framework.Infrastructure.Scheduling.Dashboard;
using Pivot.Framework.Infrastructure.Abstraction.Scheduling.Services;

namespace Pivot.Framework.Infrastructure.Scheduling.Extensions;

public static class HangfireExtensions
{
	/// <summary>
	/// Adds Hangfire services and configures the dashboard using a SQL Server database connection string from configuration.
	/// </summary>
	/// <param name="services">The service collection to add Hangfire to.</param>
	/// <param name="configuration">The application configuration to retrieve the connection string from.</param>
	/// <param name="connectionStringKey">The key in the configuration where the Hangfire connection string is stored.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddHangfireWithDashboard(this IServiceCollection services, IConfiguration configuration, string connectionStringKey = "HangfireConnection")
	{
		var connectionString = configuration.GetConnectionString(connectionStringKey);

		// Configure Hangfire with SQL Server storage.
		services.AddHangfire(config =>
		{
			config.UseSqlServerStorage(connectionString);
		});

		// Add the Hangfire server and dashboard.
		services.AddHangfireServer();

		return services;
	}

	/// <summary>
	/// Configures and mounts the Hangfire dashboard.
	/// </summary>
	/// <remarks>
	/// By default the dashboard is protected by <see cref="HangfireDashboardAuthorizationFilter"/>,
	/// which requires the incoming request to carry a valid Keycloak JWT bearer token
	/// (i.e. <c>HttpContext.User.Identity.IsAuthenticated == true</c>).
	/// This aligns the Hangfire dashboard with every other <c>[Authorize]</c>-protected
	/// endpoint in the application — no anonymous access is permitted.
	///
	/// To further restrict access to a specific Keycloak role, replace the default
	/// filter with a role-aware instance:
	/// <code>
	/// app.UseHangfireDashboardWithOptions(opts =>
	/// {
	///     opts.Authorization = new[]
	///     {
	///         new HangfireDashboardAuthorizationFilter(requiredRole: "admin")
	///     };
	/// });
	/// </code>
	/// </remarks>
	/// <param name="app">The application builder to configure the dashboard for.</param>
	/// <param name="configureDashboard">Optional action to override dashboard options.</param>
	public static void UseHangfireDashboardWithOptions(
		this IApplicationBuilder app,
		Action<DashboardOptions>? configureDashboard = null)
	{
		// Default: require a valid Keycloak JWT — any authenticated user may access the dashboard.
		var dashboardOptions = new DashboardOptions
		{
			Authorization = new IDashboardAuthorizationFilter[]
			{
				new HangfireDashboardAuthorizationFilter()
			},
			DarkModeEnabled = false
		};

		// Allow the caller to tighten (or, intentionally, widen) the defaults.
		configureDashboard?.Invoke(dashboardOptions);

		app.UseHangfireDashboard(options: dashboardOptions);
	}

	/// <summary>
	/// Registers the RecurringJobManager with the DI container to support any parameter and result type dynamically.
	/// </summary>
	/// <typeparam name="TIdentifier">The type of the unique identifier for jobs.</typeparam>
	/// <param name="services">The service collection to add the RecurringJobManager to.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddRecurringJobManager<TIdentifier>(this IServiceCollection services)
	{
		// Register the generic RecurringJobManager to allow dynamic parameter and result types.
		services.AddScoped(typeof(IRecurringJobService<,,>), typeof(RecurringJobService<,,>));
		return services;
	}
}
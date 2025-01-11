using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Scheduling.Services;
using Templates.Core.Infrastructure.Abstraction.Scheduling.Services;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;

namespace Templates.Core.Infrastructure.Scheduling.Extensions;

public static class HangfireExtensions
{
	/// <summary>
	/// Adds Hangfire services and configures the dashboard using a SQL Server database connection string from configuration.
	/// </summary>
	/// <param name="services">The service collection to add Hangfire to.</param>
	/// <param name="configuration">The application configuration to retrieve the connection string from.</param>
	/// <param name="connectionStringKey">The key in the configuration where the Hangfire connection string is stored.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddHangfireWithDashboard(this IServiceCollection services,	IConfiguration configuration, string connectionStringKey = "HangfireConnection")
	{
		// Configure Hangfire with SQL Server storage.
		services.AddHangfire(config =>
		{
			config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				  .UseSimpleAssemblyNameTypeSerializer()
				  .UseDefaultTypeSerializer()
				  .UseSqlServerStorage(connectionStringKey, new SqlServerStorageOptions
				  {
					  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
					  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
					  QueuePollInterval = TimeSpan.FromSeconds(15),
					  UseRecommendedIsolationLevel = true,
					  DisableGlobalLocks = true // Recommended for SQL Server performance.
				  });
		});

		// Add the Hangfire server and dashboard.
		services.AddHangfireServer();

		return services;
	}

	/// Configures the Hangfire dashboard with custom options.
	/// </summary>
	/// <param name="app">The application builder to configure the dashboard for.</param>
	/// <param name="configureDashboard">Optional action to configure dashboard options.</param>
	public static void UseHangfireDashboardWithOptions(
		this IApplicationBuilder app,
		Action<DashboardOptions>? configureDashboard = null)
	{
		// Set default dashboard options.
		var dashboardOptions = new DashboardOptions
		{
			Authorization = Array.Empty<IDashboardAuthorizationFilter>(),
			DarkModeEnabled = false
		};

		// Apply custom configurations if provided.
		configureDashboard?.Invoke(dashboardOptions);

		// Use Hangfire dashboard with the configured options.
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
		services.AddScoped(typeof(IRecurringJobService<,,>), typeof(IRecurringJobService<,,>));
		return services;
	}
}
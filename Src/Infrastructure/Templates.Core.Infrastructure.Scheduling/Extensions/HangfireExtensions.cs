using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Scheduling.Services;
using Templates.Core.Infrastructure.Abstraction.Scheduling.Services;

namespace Templates.Core.Infrastructure.Scheduling.Extensions;

public static class HangfireExtensions
{
	/// <summary>
	/// Adds Hangfire services and configures the dashboard.
	/// </summary>
	/// <param name="services">The service collection to add Hangfire to.</param>
	/// <param name="configureOptions">Optional action to configure Hangfire settings.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddHangfireWithDashboard(this IServiceCollection services, Action<IGlobalConfiguration>? configureOptions = null)
	{
		// Configure Hangfire with MemoryStorage by default (can be replaced with other storage options).
		services.AddHangfire(config =>
		{
			config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				  .UseSimpleAssemblyNameTypeSerializer()
				  .UseDefaultTypeSerializer()
				  .UseSqlServerStorage("TemplatesHangfireDbConnection", new SqlServerStorageOptions
				  {
					  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
					  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
					  QueuePollInterval = TimeSpan.FromSeconds(15),
					  UseRecommendedIsolationLevel = true,
					  DisableGlobalLocks = true // Recommended for SQL Server performance.
				  });

			// Apply additional configuration if provided.
			configureOptions?.Invoke(config);
		});

		// Add the Hangfire server and dashboard.
		services.AddHangfireServer();
		services.AddHangfireWithDashboard();

		return services;
	}

	/// <summary>
	/// Registers the RecurringJobManager with the DI container.
	/// </summary>
	/// <typeparam name="TIdentifier">The type of the unique identifier for jobs.</typeparam>
	/// <typeparam name="TParams">The type of the parameters for the job function.</typeparam>
	/// <typeparam name="TValue">The return type of the job function result.</typeparam>
	/// <param name="services">The service collection to add the RecurringJobManager to.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddRecurringJobManager<TIdentifier, TParams, TValue>(this IServiceCollection services)
	{
		services.AddScoped<IRecurringJobService<TIdentifier, TParams, TValue>, RecurringJobService<TIdentifier, TParams, TValue>>();
		return services;
	}
}
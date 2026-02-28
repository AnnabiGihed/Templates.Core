using Scrutor;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pivot.Framework.Tools.DependencyInjection;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for service installers that centralizes dependency injection registration behavior.
///              Provides a convention-based registration helper using Scrutor to scan assemblies and
///              register concrete implementations against their matching interfaces
///              (e.g. OnlineEducationEmailService : IOnlineEducationEmailService).
/// </summary>
public abstract class BaseServiceInstaller
{
	/// <summary>
	/// Performs convention-based registrations by scanning the given assemblies and registering
	/// concrete classes against their matching interfaces.
	/// </summary>
	/// <param name="services">The service collection to register dependencies into.</param>
	/// <param name="configuration">
	/// The application configuration. Not used by the base method but kept to support derived installers
	/// that use configuration-driven registration.
	/// </param>
	/// <param name="assemblies">Assemblies to scan for services.</param>
	/// <param name="lifetime">The lifetime to apply to registered services. Default is Scoped.</param>
	/// <param name="strategy">
	/// The Scrutor registration strategy when a service already exists. If null, defaults to Skip.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="assemblies"/> is empty.</exception>
	protected void IncludeConventionBasedRegistrations(IServiceCollection services, IConfiguration configuration, Assembly[] assemblies, ServiceLifetime lifetime = ServiceLifetime.Scoped, RegistrationStrategy? strategy = null)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(assemblies);

		if (assemblies.Length == 0)
			throw new ArgumentException("At least one assembly must be provided for scanning.", nameof(assemblies));

		_ = configuration;

		var effectiveStrategy = strategy ?? RegistrationStrategy.Skip;

		services.Scan(scan =>
		{
			var registration = scan
				.FromAssemblies(assemblies)
				.AddClasses(classes => classes.Where(t =>
					t.IsClass &&
					!t.IsAbstract &&
					!t.ContainsGenericParameters))
				.UsingRegistrationStrategy(effectiveStrategy)
				.AsMatchingInterface();

			switch (lifetime)
			{
				case ServiceLifetime.Singleton:
					registration.WithSingletonLifetime();
					break;

				case ServiceLifetime.Transient:
					registration.WithTransientLifetime();
					break;

				default:
					registration.WithScopedLifetime();
					break;
			}
		});
	}
}

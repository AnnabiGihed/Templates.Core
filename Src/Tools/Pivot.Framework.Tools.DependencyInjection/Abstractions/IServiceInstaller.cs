using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pivot.Framework.Tools.DependencyInjection.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines the contract for dependency injection installers.
///              Implementations encapsulate service registration for a given module or layer,
///              typically called from the application's composition root.
/// </summary>
public interface IServiceInstaller
{
	/// <summary>
	/// Registers module services into the dependency injection container.
	/// </summary>
	/// <param name="services">The service collection to register dependencies into.</param>
	/// <param name="configuration">The application configuration used to drive registrations.</param>
	/// <param name="includeConventionBasedRegistration">
	/// When <c>true</c>, the installer should also apply convention-based registrations (e.g., Scrutor scanning)
	/// in addition to explicit registrations.
	/// </param>
	void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true);
}

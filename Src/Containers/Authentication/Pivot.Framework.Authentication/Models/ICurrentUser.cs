using System.Security.Claims;

namespace Pivot.Framework.Authentication.Models;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Provides strongly-typed access to the authenticated user's claims.
///              Register via <see cref="Extensions.KeycloakAuthenticationExtensions.AddKeycloakAuthentication"/>.
///
///              Inject <see cref="ICurrentUser"/> in controllers, services, or minimal-API handlers.
/// </summary>
public interface ICurrentUser
{
	/// <summary>Whether the request is authenticated.</summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Keycloak subject (sub) claim parsed as a <see cref="Guid"/>.
	/// Keycloak always issues UUIDs for the sub claim, making this a safe parse.
	/// Returns <c>null</c> when the request is unauthenticated or the claim is absent/malformed.
	/// </summary>
	Guid? UserId { get; }

	/// <summary>Keycloak preferred_username.</summary>
	string? Username { get; }

	/// <summary>User's email address.</summary>
	string? Email { get; }

	/// <summary>Display name (first + last, or preferred_username fallback).</summary>
	string? DisplayName { get; }

	/// <summary>All realm + client roles flattened.</summary>
	IReadOnlyList<string> Roles { get; }

	/// <summary>Returns true if the user has the given role.</summary>
	bool IsInRole(string role);

	/// <summary>Raw ClaimsPrincipal if you need anything else.</summary>
	ClaimsPrincipal? Principal { get; }
}
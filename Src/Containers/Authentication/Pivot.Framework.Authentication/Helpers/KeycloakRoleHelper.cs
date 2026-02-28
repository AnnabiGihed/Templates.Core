using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Pivot.Framework.Authentication.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Shared utility that copies Keycloak role claims into standard .NET
///              <see cref="ClaimTypes.Role"/> claims so [Authorize(Roles = "...")] works
///              across all platform-specific authentication packages (AspNetCore, Blazor, MAUI).
///
///              Keycloak places roles in two locations inside the JWT:
///              - realm_access.roles  → realm-level roles  (e.g. "admin")
///              - resource_access.{clientId}.roles → client-level roles (e.g. "read:routes")
/// </summary>
public static class KeycloakRoleHelper
{
	#region Public Methods
	/// <summary>
	/// Flattens Keycloak realm and resource-access roles into <see cref="ClaimTypes.Role"/> claims.
	/// </summary>
	/// <param name="identity">The claims identity to mutate.</param>
	/// <param name="logger">Optional logger — when provided, malformed JSON claims are logged at Debug level.</param>
	public static void FlattenRoles(ClaimsIdentity identity, ILogger? logger = null)
	{
		FlattenRealmRoles(identity, logger);
		FlattenResourceRoles(identity, logger);
	}
	#endregion

	#region Private helpers
	private static void FlattenRealmRoles(ClaimsIdentity identity, ILogger? logger)
	{
		var claim = identity.FindFirst("realm_access");
		if (claim is null)
			return;

		try
		{
			using var doc = JsonDocument.Parse(claim.Value);
			if (!doc.RootElement.TryGetProperty("roles", out var roles))
				return;

			foreach (var role in roles.EnumerateArray())
			{
				var roleValue = role.GetString();
				if (!string.IsNullOrWhiteSpace(roleValue))
					identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
			}
		}
		catch (JsonException ex)
		{
			logger?.LogDebug(ex, "Keycloak: could not parse realm_access claim — check your Keycloak token mapper configuration.");
		}
	}

	private static void FlattenResourceRoles(ClaimsIdentity identity, ILogger? logger)
	{
		var claim = identity.FindFirst("resource_access");
		if (claim is null)
			return;

		try
		{
			using var doc = JsonDocument.Parse(claim.Value);
			foreach (var clientEntry in doc.RootElement.EnumerateObject())
			{
				if (!clientEntry.Value.TryGetProperty("roles", out var roles))
					continue;

				foreach (var role in roles.EnumerateArray())
				{
					var roleValue = role.GetString();
					if (!string.IsNullOrWhiteSpace(roleValue))
						identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
				}
			}
		}
		catch (JsonException ex)
		{
			logger?.LogDebug(ex, "Keycloak: could not parse resource_access claim — check your Keycloak token mapper configuration.");
		}
	}
	#endregion
}
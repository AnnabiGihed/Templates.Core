using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Templates.Core.Authentication.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Transforms Keycloak-specific JWT claims into standard .NET role claims.
///              Keycloak places roles in two locations:
///              - realm_access.roles → realm-level roles (e.g. "admin")
///              - resource_access.{clientId}.roles → client-level roles (e.g. "read:routes")
///              This transformer copies both into ClaimTypes.Role so that
///              [Authorize(Roles = "admin")] works out of the box.
/// </summary>
internal static class KeycloakClaimsTransformer
{
	#region Public Methods
	public static void FlattenRoles(TokenValidatedContext ctx)
	{
		if (ctx.Principal?.Identity is not ClaimsIdentity identity)
			return;

		FlattenRealmRoles(identity);
		FlattenResourceRoles(identity);
	}
	#endregion

	#region Private helpers
	private static void FlattenRealmRoles(ClaimsIdentity identity)
	{
		var realmAccessClaim = identity.FindFirst("realm_access");
		if (realmAccessClaim is null)
			return;

		try
		{
			using var doc = JsonDocument.Parse(realmAccessClaim.Value);
			if (!doc.RootElement.TryGetProperty("roles", out var roles))
				return;

			foreach (var role in roles.EnumerateArray())
			{
				var roleValue = role.GetString();
				if (!string.IsNullOrWhiteSpace(roleValue))
					identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
			}
		}
		catch (JsonException)
		{
		}
	}

	private static void FlattenResourceRoles(ClaimsIdentity identity)
	{
		var resourceAccessClaim = identity.FindFirst("resource_access");
		if (resourceAccessClaim is null)
			return;

		try
		{
			using var doc = JsonDocument.Parse(resourceAccessClaim.Value);
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
		catch (JsonException)
		{
		}
	}
	#endregion
}
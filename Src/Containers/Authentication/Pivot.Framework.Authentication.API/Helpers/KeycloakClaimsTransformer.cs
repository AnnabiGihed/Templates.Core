using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Pivot.Framework.Authentication.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Pivot.Framework.Authentication.AspNetCore.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Transforms Keycloak-specific JWT claims into standard .NET role claims.
///              Keycloak places roles in two locations:
///              - realm_access.roles → realm-level roles (e.g. "admin")
///              - resource_access.{clientId}.roles → client-level roles (e.g. "read:routes")
///              This transformer copies both into ClaimTypes.Role so that
///              [Authorize(Roles = "admin")] works out of the box.
///
///              Flattening logic is shared via <see cref="KeycloakRoleHelper.FlattenRoles"/>
///              across all platform-specific authentication packages.
/// </summary>
internal static class KeycloakClaimsTransformer
{
	#region Public Methods
	public static void FlattenRoles(TokenValidatedContext ctx)
	{
		if (ctx.Principal?.Identity is not ClaimsIdentity identity)
			return;

		var logger = ctx.HttpContext.RequestServices
			.GetService<ILogger<JwtBearerEvents>>();

		KeycloakRoleHelper.FlattenRoles(identity, logger);
	}
	#endregion
}
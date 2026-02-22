using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Templates.Core.Authentication.Models;

/// <inheritdoc />
public sealed class CurrentUser : ICurrentUser
{
	#region Dependencies
	private readonly IHttpContextAccessor _accessor;
	#endregion

	#region Constructor
	public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;
	#endregion

	#region Private helpers
	private ClaimsPrincipal? User => _accessor.HttpContext?.User;
	#endregion

	#region ICurrentUser
	public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

	public ClaimsPrincipal? Principal => User;

	/// <inheritdoc />
	/// <remarks>
	/// Keycloak's sub claim is always a UUID (e.g. "a3f1c2d4-9e1b-4c7a-bb82-f1234567890a").
	/// We parse it to <see cref="Guid"/> here so consumers never deal with raw strings.
	/// Returns <c>null</c> if the claim is absent or, in practice-impossible edge cases,
	/// malformed.
	/// </remarks>
	public Guid? UserId
	{
		get
		{
			var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User?.FindFirstValue("sub");

			return Guid.TryParse(raw, out var guid) ? guid : null;
		}
	}

	public string? Username => User?.FindFirstValue("preferred_username") ?? User?.FindFirstValue(ClaimTypes.Name);

	public string? Email =>	User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");

	public string? DisplayName
	{
		get
		{
			var name = User?.FindFirstValue("name");
			if (name is not null) return name;

			var given = User?.FindFirstValue("given_name");
			var family = User?.FindFirstValue("family_name");
			if (given is not null)
				return string.IsNullOrWhiteSpace(family) ? given : $"{given} {family}".Trim();

			return Username;
		}
	}

	public IReadOnlyList<string> Roles => User?.Claims
			 .Where(c => c.Type == ClaimTypes.Role)
			 .Select(c => c.Value)
			 .ToList() ?? [];

	public bool IsInRole(string role) => User?.IsInRole(role) == true;
	#endregion
}
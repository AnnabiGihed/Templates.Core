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
	/// <inheritdoc />
	public Guid? UserId
	{
		get
		{
			var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User?.FindFirstValue("sub");

			return Guid.TryParse(raw, out var guid) ? guid : null;
		}
	}

	public string? DisplayName
	{
		get
		{
			var name = User?.FindFirstValue("name");
			if (name is not null)
				return name;

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

	public ClaimsPrincipal? Principal => User;

	public bool IsInRole(string role) => User?.IsInRole(role) == true;

	public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

	public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");

	public string? Username => User?.FindFirstValue("preferred_username") ?? User?.FindFirstValue(ClaimTypes.Name);
	#endregion
}
using System.Security.Claims;

namespace Templates.Core.Authentication.Events;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Event arguments for <see cref="IKeycloakAuthService.AuthStateChanged"/>,
///              carrying the current authentication flag and the associated user principal.
/// </summary>
public sealed class AuthStateChangedEventArgs : EventArgs
{
	#region Properties
	public bool IsAuthenticated { get; }

	public ClaimsPrincipal? User { get; }
	#endregion

	#region Constructor
	public AuthStateChangedEventArgs(bool isAuthenticated, ClaimsPrincipal? user)
	{
		IsAuthenticated = isAuthenticated;
		User = user;
	}
	#endregion
}
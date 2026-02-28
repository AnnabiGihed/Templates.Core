using System.Security.Claims;
using Pivot.Framework.Authentication.Events;
using Pivot.Framework.Authentication.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Pivot.Framework.Authentication.Maui.AuthenticationStateProvider;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Blazor <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"/> backed by <see cref="IKeycloakAuthService"/>.
///              Integrates Keycloak authentication state into Blazor primitives such as
///              <c>&lt;AuthorizeView&gt;</c>, <c>[Authorize]</c> and <c>CascadingAuthenticationState</c>.
/// </summary>
public sealed class KeycloakAuthStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, IDisposable
{
	#region Fields
	private AuthenticationState _current;
	#endregion

	#region Constants
	private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
	#endregion

	#region Dependencies
	private readonly IKeycloakAuthService _auth;
	#endregion

	#region Constructor
	public KeycloakAuthStateProvider(IKeycloakAuthService auth)
	{
		_auth = auth;
		_auth.AuthStateChanged += OnAuthStateChanged;
		_current = _auth.IsAuthenticated && _auth.User is not null ? new AuthenticationState(_auth.User) : Anonymous;
	}
	#endregion

	#region Public Methods
	/// <inheritdoc />
	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return Task.FromResult(_current);
	}
	#endregion

	#region Private helpers
	private void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
	{
		_current = e.IsAuthenticated && e.User is not null ? new AuthenticationState(e.User) : Anonymous;

		NotifyAuthenticationStateChanged(Task.FromResult(_current));
	}
	#endregion

	#region IDisposable
	/// <inheritdoc />
	public void Dispose()
	{
		_auth.AuthStateChanged -= OnAuthStateChanged;
	}
	#endregion
}
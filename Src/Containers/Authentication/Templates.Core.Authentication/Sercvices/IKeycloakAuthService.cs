using System.Security.Claims;
using Templates.Core.Authentication.Events;

namespace Templates.Core.Authentication.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines the contract for the Keycloak authentication service used by MAUI clients.
///              Exposes authentication state changes, current user claims and the main auth flows
///              (login, logout, access-token retrieval with refresh, and session restore).
/// </summary>
public interface IKeycloakAuthService
{
	/// <summary>
	/// Observable auth state — fires whenever login/logout/refresh happens.
	/// </summary>
	event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

	/// <summary>
	/// Whether the user is currently authenticated (has a valid access token).
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Claims from the current ID token / access token.
	/// </summary>
	ClaimsPrincipal? User { get; }

	/// <summary>
	/// Starts the browser-based Authorization Code + PKCE flow.
	/// Redirects to Keycloak login page and exchanges the code for tokens.
	/// </summary>
	Task<bool> LoginAsync(CancellationToken ct = default);

	/// <summary>
	/// Logs the user out: revokes the refresh token and clears local storage.
	/// Also opens the Keycloak end-session URL to log out of the SSO session.
	/// </summary>
	Task LogoutAsync(CancellationToken ct = default);

	/// <summary>
	/// Returns a valid access token, refreshing silently if needed.
	/// Throws <see cref="UnauthorizedAccessException"/> if not logged in.
	/// </summary>
	Task<string> GetAccessTokenAsync(CancellationToken ct = default);

	/// <summary>
	/// Tries to restore the session from SecureStorage on app start.
	/// Call this in your app shell before navigating.
	/// </summary>
	Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
}
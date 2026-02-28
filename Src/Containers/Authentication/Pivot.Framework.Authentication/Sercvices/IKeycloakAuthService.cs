using System.Security.Claims;
using Pivot.Framework.Authentication.Events;

namespace Pivot.Framework.Authentication.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines the contract for the Keycloak authentication service used by both
///              MAUI and Blazor clients. Exposes authentication state changes, current user
///              claims and the main auth flows (login, logout, access-token retrieval with
///              refresh, session restore, and forced refresh).
/// </summary>
public interface IKeycloakAuthService
{
	/// <summary>
	/// Observable auth state — fires whenever login/logout/refresh happens.
	/// </summary>
	event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

	/// <summary>
	/// Whether the user is currently authenticated (has a valid, non-expired access token).
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Claims from the current access token.
	/// </summary>
	ClaimsPrincipal? User { get; }

	/// <summary>
	/// Logs the user out: revokes the refresh token, clears local/session storage,
	/// and redirects to the Keycloak end-session endpoint.
	/// </summary>
	Task LogoutAsync(CancellationToken ct = default);

	/// <summary>
	/// Starts the platform-specific Authorization Code + PKCE login flow.
	/// On MAUI: opens the system browser via WebAuthenticator.
	/// On Blazor: redirects to Keycloak; the result arrives via HandleCallbackAsync.
	/// </summary>
	Task<bool> LoginAsync(CancellationToken ct = default);

	/// <summary>
	/// Forces an unconditional token refresh regardless of current expiry state.
	/// Call this after receiving a 401 from the API to ensure the token is fresh.
	/// Throws <see cref="UnauthorizedAccessException"/> if no refresh token is available.
	/// </summary>
	Task<string> ForceRefreshAsync(CancellationToken ct = default);

	/// <summary>
	/// Returns a valid access token, refreshing silently if the current one is near expiry.
	/// Throws <see cref="UnauthorizedAccessException"/> if not logged in or refresh is impossible.
	/// </summary>
	Task<string> GetAccessTokenAsync(CancellationToken ct = default);

	/// <summary>
	/// Tries to restore a persisted session on app/page start.
	/// Call this in your app shell or layout before navigating.
	/// </summary>
	Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
}
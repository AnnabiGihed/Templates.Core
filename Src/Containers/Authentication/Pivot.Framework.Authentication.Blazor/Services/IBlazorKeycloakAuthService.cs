using Pivot.Framework.Authentication.Services;

namespace Pivot.Framework.Authentication.Blazor.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Extends <see cref="IKeycloakAuthService"/> with Blazor-specific operations
///              needed for the redirect-based PKCE flow and server-side session management.
/// </summary>
public interface IBlazorKeycloakAuthService : IKeycloakAuthService
{
	/// <summary>
	/// Initialises the service from the session cookie present on the current request.
	/// Call this early in the Blazor circuit lifecycle (e.g. in a layout's OnInitializedAsync).
	/// </summary>
	Task InitialiseFromCookieAsync(CancellationToken ct = default);

	/// <summary>
	/// Exchanges the authorization code returned by Keycloak at the /auth/callback page.
	/// Returns the URL to redirect to after a successful login (the page the user was on
	/// when they triggered login), or null on failure.
	/// </summary>
	/// <param name="code">The authorization code from the query string.</param>
	/// <param name="returnedState">The state parameter from the query string (for CSRF validation).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// The return URL on success (e.g. "/dashboard"), or <c>null</c> on failure
	/// (state mismatch, nonce failure, token exchange error, etc.).
	/// </returns>
	Task<string?> HandleCallbackAsync(string code, string returnedState, CancellationToken ct = default);
}
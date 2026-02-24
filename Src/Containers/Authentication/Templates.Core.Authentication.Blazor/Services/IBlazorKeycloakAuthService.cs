using Templates.Core.Authentication.Services;

namespace Templates.Core.Authentication.Blazor.Services;

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
	/// </summary>
	/// <param name="code">The authorization code from the query string.</param>
	/// <param name="returnedState">The state parameter from the query string (for CSRF validation).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns><c>true</c> on success; <c>false</c> on state mismatch, nonce failure, or exchange error.</returns>
	Task<bool> HandleCallbackAsync(string code, string returnedState, CancellationToken ct = default);
}
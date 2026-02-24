namespace Templates.Core.Authentication.Blazor.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Server-side session storage contract for the Blazor Keycloak auth service.
///              Tokens never leave the server — only an opaque, cryptographically random
///              session ID is written to the browser as an HttpOnly cookie.
/// </summary>
public interface IBlazorTokenSessionStore
{
	/// <summary>
	/// Removes the token session. Call on logout.
	/// </summary>
	Task RemoveAsync(string sessionId, CancellationToken ct = default);

	/// <summary>
	/// Returns the token session for the given session ID, or <c>null</c> if it does not exist.
	/// </summary>
	Task<BlazorTokenSession?> GetAsync(string sessionId, CancellationToken ct = default);

	/// <summary>
	/// Persists a token set on the server keyed by an opaque session ID.
	/// </summary>
	Task SaveAsync(string sessionId, BlazorTokenSession session, CancellationToken ct = default);
}
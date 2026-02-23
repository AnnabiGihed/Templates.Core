namespace Templates.Core.Authentication.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines the contract for the Keycloak token storage layer used by MAUI clients.
///              Responsible for securely persisting, retrieving and clearing the OAuth2 token set.
/// </summary>
public interface IKeycloakTokenStorage
{
	/// <summary>
	/// Clears any persisted Keycloak tokens from storage.
	/// </summary>
	Task ClearAsync(CancellationToken ct = default);

	/// <summary>
	/// Retrieves the persisted Keycloak token set from storage.
	/// Returns <c>null</c> if no token set is available or if it cannot be deserialized.
	/// </summary>
	Task<KeycloakTokenSet?> GetAsync(CancellationToken ct = default);

	/// <summary>
	/// Persists the provided Keycloak token set to storage, overwriting any existing value.
	/// </summary>
	Task SaveAsync(KeycloakTokenSet tokens, CancellationToken ct = default);
}
using System.Text.Json;
using Templates.Core.Authentication.Storage;

namespace Templates.Core.Authentication.Maui.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Persists the Keycloak token set using MAUI's platform SecureStorage.
///              On Android/iOS this maps to the OS keychain. On Windows it uses DPAPI.
///              All methods are thread-safe.
/// </summary>
public sealed class KeycloakTokenStorage : IKeycloakTokenStorage
{
	#region Constants
	private const string StorageKey = "keycloak_token_set";
	#endregion

	#region Fields
	private readonly SemaphoreSlim _lock = new(1, 1);
	#endregion

	#region Public Methods
	/// <inheritdoc />
	public async Task<KeycloakTokenSet?> GetAsync(CancellationToken ct = default)
	{
		await _lock.WaitAsync(ct);
		try
		{
			var json = await SecureStorage.Default.GetAsync(StorageKey);
			if (string.IsNullOrEmpty(json))
				return null;

			return JsonSerializer.Deserialize<KeycloakTokenSet>(json);
		}
		catch
		{
			return null;
		}
		finally
		{
			_lock.Release();
		}
	}

	/// <inheritdoc />
	public async Task SaveAsync(KeycloakTokenSet tokens, CancellationToken ct = default)
	{
		await _lock.WaitAsync(ct);
		try
		{
			var json = JsonSerializer.Serialize(tokens);
			await SecureStorage.Default.SetAsync(StorageKey, json);
		}
		finally
		{
			_lock.Release();
		}
	}

	/// <inheritdoc />
	public async Task ClearAsync(CancellationToken ct = default)
	{
		await _lock.WaitAsync(ct);
		try
		{
			SecureStorage.Default.Remove(StorageKey);
			await Task.CompletedTask;
		}
		finally
		{
			_lock.Release();
		}
	}
	#endregion
}
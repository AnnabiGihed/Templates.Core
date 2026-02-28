using System.Text.Json;
using Microsoft.JSInterop;
using Pivot.Framework.Authentication.Storage;

namespace Pivot.Framework.Authentication.Blazor.Storage;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Persists the Keycloak token set using browser localStorage via IJSRuntime.
///              Mirrors the MAUI SecureStorage implementation but targets Blazor WebAssembly
///              and Blazor Server (where JS interop is available).
///              Note: localStorage is not encrypted — for higher security, combine with
///              a server-side session or use sessionStorage by swapping the key prefix.
/// </summary>
public sealed class KeycloakTokenStorage : IKeycloakTokenStorage
{
	#region Constants
	private const string StorageKey = "keycloak_token_set";
	#endregion

	#region Dependencies
	private readonly IJSRuntime _js;
	private readonly SemaphoreSlim _lock = new(1, 1);
	#endregion

	#region Constructor
	public KeycloakTokenStorage(IJSRuntime js)
	{
		_js = js;
	}
	#endregion

	#region Public Methods
	/// <inheritdoc />
	public async Task ClearAsync(CancellationToken ct = default)
	{
		await _lock.WaitAsync(ct);
		try
		{
			await _js.InvokeVoidAsync("localStorage.removeItem", ct, StorageKey);
		}
		finally
		{
			_lock.Release();
		}
	}

	/// <inheritdoc />
	public async Task<KeycloakTokenSet?> GetAsync(CancellationToken ct = default)
	{
		await _lock.WaitAsync(ct);
		try
		{
			var json = await _js.InvokeAsync<string?>("localStorage.getItem", ct, StorageKey);
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
			await _js.InvokeVoidAsync("localStorage.setItem", ct, StorageKey, json);
		}
		finally
		{
			_lock.Release();
		}
	}
	#endregion
}
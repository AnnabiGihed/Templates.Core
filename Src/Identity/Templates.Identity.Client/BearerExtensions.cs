using IdentityModel.Client;
using Microsoft.AspNetCore.Components.Authorization;

namespace Templates.Identity.Client;

public static class BearerExtensions
{

	public static async void AddBearerTokenAsync(this HttpClient httpClient,
												 AuthenticationStateProvider authenticationStateProvider,
												 ServerAuthenticationStateCache serverAuthenticationStateCache)
	{
		var state = await authenticationStateProvider.GetAuthenticationStateAsync();
		var sid = state.User.FindFirst("sid")?.Value;
		var data = serverAuthenticationStateCache.Get(sid);
		httpClient.SetBearerToken(data.AccessToken);
	}

}

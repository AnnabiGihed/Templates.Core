using Microsoft.AspNetCore.Components.Authorization;

namespace Templates.Identity.Client;

public class AccessTokenService : IAccessTokenService
{
	private readonly AuthenticationStateProvider _stateProvider;
	private readonly ServerAuthenticationStateCache _cache;

	public AccessTokenService(AuthenticationStateProvider stateProvider, ServerAuthenticationStateCache cache)
	{
		_stateProvider = stateProvider;
		_cache = cache;
	}

	public async Task<DateTimeOffset> GetExpirationTime()
	{
		var state = await _stateProvider.GetAuthenticationStateAsync();
		if (state.User.Identity.IsAuthenticated)
		{
			var sid = state.User.Claims
				.Where(c => c.Type.Equals("sid"))
				.Select(c => c.Value)
				.FirstOrDefault();
			if (sid != null && _cache.HasSubjectId(sid))
			{
				return _cache.Get(sid).ExpireAt;
			}
		}

		return DateTimeOffset.MaxValue;
	}
}

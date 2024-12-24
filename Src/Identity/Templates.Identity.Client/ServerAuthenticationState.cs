using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Authorization;

namespace Templates.Identity.Client;

public class ServerAuthenticationState : RevalidatingServerAuthenticationStateProvider
{
	private readonly ServerAuthenticationStateCache _cache;
	private readonly ILogger _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;
	private readonly IDiscoveryCache _discoveryCache;

	public ServerAuthenticationState(
		ILoggerFactory loggerFactory,
		ServerAuthenticationStateCache cache,
		IHttpClientFactory httpClientFactory,
		IConfiguration configuration,
		IDiscoveryCache discoveryCache
		)
		: base(loggerFactory)
	{
		_cache = cache;
		_logger = loggerFactory.CreateLogger(typeof(ServerAuthenticationState).Name);
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
		_discoveryCache = discoveryCache;
	}

	protected override TimeSpan RevalidationInterval
		=> TimeSpan.FromSeconds(10); // TODO read from config

	protected async override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
	{
		var sid = authenticationState.User.Claims
				.Where(c => c.Type.Equals("sid"))
				.Select(c => c.Value)
				.FirstOrDefault();

		var name = authenticationState.User.Claims
					.Where(c => c.Type.Equals("name"))
					.Select(c => c.Value)
					.FirstOrDefault() ?? string.Empty;

		_logger.LogInformation($"Validate: {name} / {sid}");

		if (sid != null && _cache.HasSubjectId(sid))
		{
			var data = _cache.Get(sid);

			_logger.LogDebug($"NowUtc: {DateTimeOffset.UtcNow.ToString("o")}");
			_logger.LogDebug($"ExpUtc: {data.Expiration.ToString("o")}");

			if (DateTimeOffset.UtcNow >= data.Expiration)
			{
				_logger.LogDebug($"{name}/{sid} EXPIRED !");
				_cache.Remove(sid);
				return false;
			}

			if (data.RefreshAt < DateTimeOffset.UtcNow)
			{
				await RefreshAccessToken(data);
			}
		}
		else
		{
			_logger.LogDebug($"{sid} not in cache");
		}

		return true;
	}

	private async Task RefreshAccessToken(ServerAuthenticationData data)
	{
		_logger.LogInformation($"User {data.SubjectId} : refreshing API access token");

		var client = _httpClientFactory.CreateClient();
		var disco = await _discoveryCache.GetAsync();
		if (disco.IsError) return;
		_logger.LogDebug("...discovery complete");

		var tokenResponse = await client.RequestRefreshTokenAsync(
			new RefreshTokenRequest
			{
				Address = disco.TokenEndpoint,
				ClientId = _configuration.GetValue<string>("oidc:ClientId"),
				ClientSecret = _configuration.GetValue<string>("oidc:ClientSecret"),
				RefreshToken = data.RefreshToken,
				ClientCredentialStyle = ClientCredentialStyle.PostBody,
			});
		if (tokenResponse.IsError)
		{
			_logger.LogError($"User {data.SubjectId} : failed to refresh token", tokenResponse.Exception);
			return;
		}
		_logger.LogDebug($"User {data.SubjectId} : refresh token complete");

		data.AccessToken = tokenResponse.AccessToken;
		data.RefreshToken = tokenResponse.RefreshToken;
		data.RefreshAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn / 2);
	}
}
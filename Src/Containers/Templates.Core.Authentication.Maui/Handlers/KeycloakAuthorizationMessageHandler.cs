using Microsoft.Extensions.Logging;
using Templates.Core.Authentication.Maui.Services;

namespace Templates.Core.Authentication.Maui.Handlers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : HTTP <see cref="DelegatingHandler"/> that attaches the Keycloak access token as a Bearer header
///              on outgoing requests and handles 401 responses by attempting a one-time silent refresh and retry.
///              Intended to be registered on an <see cref="HttpClient"/> via DI.
/// </summary>
public sealed class KeycloakAuthorizationMessageHandler : DelegatingHandler
{
	#region Dependencies
	private readonly IKeycloakAuthService _auth;
	private readonly ILogger<KeycloakAuthorizationMessageHandler> _logger;
	#endregion

	#region Constructor
	public KeycloakAuthorizationMessageHandler(IKeycloakAuthService auth, ILogger<KeycloakAuthorizationMessageHandler> logger)
	{
		_auth = auth;
		_logger = logger;
	}
	#endregion

	#region Overrides
	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		if (request.Headers.Authorization is not null)
			return await base.SendAsync(request, ct);

		string? token = null;
		try
		{
			token = await _auth.GetAccessTokenAsync(ct);
		}
		catch (UnauthorizedAccessException)
		{
			_logger.LogDebug("No access token available; sending request unauthenticated.");
		}

		if (token is not null)
		{
			request.Headers.Authorization =	new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		}

		var response = await base.SendAsync(request, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && token is not null)
		{
			_logger.LogInformation("Received 401 — attempting silent token refresh and retry.");
			response.Dispose();

			try
			{
				var freshToken = await _auth.GetAccessTokenAsync(ct);
				var retryRequest = await CloneRequestAsync(request, ct);
				retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", freshToken);

				response = await base.SendAsync(retryRequest, ct);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Token refresh on 401 failed.");
			}
		}

		return response;
	}
	#endregion

	#region Private helpers
	private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, CancellationToken ct)
	{
		var clone = new HttpRequestMessage(original.Method, original.RequestUri);

		if (original.Content is not null)
		{
			var content = await original.Content.ReadAsByteArrayAsync(ct);
			clone.Content = new ByteArrayContent(content);
			foreach (var header in original.Content.Headers)
				clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		foreach (var header in original.Headers)
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

		return clone;
	}
	#endregion
}
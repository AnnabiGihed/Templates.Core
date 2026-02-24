using Microsoft.Extensions.Logging;
using Templates.Core.Authentication.Services;

namespace Templates.Core.Authentication.Handlers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : HTTP <see cref="DelegatingHandler"/> that attaches the Keycloak access token
///              as a Bearer header on outgoing requests. On a 401 response it performs exactly
///              one forced refresh via <see cref="IKeycloakAuthService.ForceRefreshAsync"/> and
///              retries the request once. A second 401 is returned as-is without further retries.
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
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var response = await base.SendAsync(request, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && token is not null)
		{
			_logger.LogInformation("Received 401 — performing forced token refresh and retrying once.");

			response.Dispose();

			try
			{
				var freshToken = await _auth.ForceRefreshAsync(ct);

				var retryRequest = await CloneRequestAsync(request, ct);
				retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", freshToken);

				response = await base.SendAsync(retryRequest, ct);

				if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
					_logger.LogWarning("Retry after forced refresh still returned 401 — session may be revoked.");
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Forced token refresh on 401 failed; returning 401.");
				response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
			}
		}

		return response;
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Creates a shallow clone of an <see cref="HttpRequestMessage"/>.
	/// HttpRequestMessage cannot be re-sent, so we must build a new one.
	/// </summary>
	private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, CancellationToken ct)
	{
		var clone = new HttpRequestMessage(original.Method, original.RequestUri);

		if (original.Content is not null)
		{
			var bytes = await original.Content.ReadAsByteArrayAsync(ct);
			clone.Content = new ByteArrayContent(bytes);

			foreach (var header in original.Content.Headers)
				clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		foreach (var header in original.Headers)
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

		clone.Version = original.Version;

		return clone;
	}
	#endregion
}
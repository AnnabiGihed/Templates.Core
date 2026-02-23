using System.Text.Json.Serialization;

namespace Templates.Core.Authentication.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents the JSON payload returned by Keycloak's token endpoint
///              for authorization_code and refresh_token grants.
///              Used to deserialize id_token, access_token, refresh_token and expiry metadata.
/// </summary>
public sealed class KeycloakTokenResponse
{
	#region Properties
	[JsonPropertyName("id_token")]
	public string? IdToken { get; init; }

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; init; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; init; }

	[JsonPropertyName("access_token")]
	public string AccessToken { get; init; } = string.Empty;
	#endregion
}
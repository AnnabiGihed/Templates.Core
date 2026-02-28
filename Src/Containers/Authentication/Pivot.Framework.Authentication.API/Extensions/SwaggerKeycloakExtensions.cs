using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pivot.Framework.Authentication.AspNetCore.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Provides Swagger / Swashbuckle extension methods to configure Swagger UI
///              with Keycloak OAuth2 Authorization Code + PKCE and apply a global security requirement.
/// </summary>
public static class SwaggerKeycloakExtensions
{
	#region Constants
	public const string SecuritySchemeName = "oauth2";
	#endregion

	#region Extensions
	/// <summary>
	/// Adds a global security requirement so every endpoint shows the lock icon.
	/// </summary>
	public static SwaggerGenOptions AddKeycloakSecurityRequirement(this SwaggerGenOptions options)
	{
		options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
		{
			[new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = []
		});

		return options;
	}

	/// <summary>
	/// Configures Swagger UI to pre-fill the Keycloak client_id and enable PKCE.
	/// Call this inside <c>app.UseSwaggerUI(c => ...)</c>.
	///
	/// Uses <see cref="SwaggerUIOptions.OAuthConfigObject"/> directly for maximum
	/// compatibility across Swashbuckle versions.
	/// </summary>
	public static SwaggerUIOptions UseKeycloakOAuth(this SwaggerUIOptions options, IServiceProvider serviceProvider)
	{
		var keycloak = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

		options.OAuthConfigObject.ScopeSeparator = " ";
		options.OAuthConfigObject.ClientId = keycloak.ClientId;
		options.OAuthConfigObject.UsePkceWithAuthorizationCodeGrant = true;
		options.OAuthConfigObject.Scopes = keycloak.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		return options;
	}

	/// <summary>
	/// Adds the Keycloak OAuth2 Authorization Code + PKCE security definition to Swagger.
	/// Resolves <see cref="KeycloakOptions"/> from the already-built DI container.
	/// </summary>
	public static SwaggerGenOptions AddKeycloakSecurityDefinition(this SwaggerGenOptions options, IServiceProvider serviceProvider)
	{
		var keycloak = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
		return options.AddKeycloakSecurityDefinition(keycloak);
	}

	/// <summary>
	/// Adds the Keycloak OAuth2 Authorization Code + PKCE security definition to Swagger.
	/// Use this overload during <c>services.AddSwaggerGen(...)</c> — it reads options directly
	/// from <paramref name="configuration"/> and avoids calling <c>BuildServiceProvider()</c>.
	/// </summary>
	public static SwaggerGenOptions AddKeycloakSecurityDefinition(this SwaggerGenOptions options, IConfiguration configuration)
	{
		var keycloak = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
			?? throw new InvalidOperationException($"Missing configuration section '{KeycloakOptions.SectionName}'.");

		return options.AddKeycloakSecurityDefinition(keycloak);
	}
	#endregion

	#region Helpers
	private static SwaggerGenOptions AddKeycloakSecurityDefinition(this SwaggerGenOptions options, KeycloakOptions keycloak)
	{
		options.AddSecurityDefinition(SecuritySchemeName, new OpenApiSecurityScheme
		{
			Type = SecuritySchemeType.OAuth2,
			Description = "Authenticate via Keycloak. Click **Authorize** and use the OAuth2 flow.",
			Flows = new OpenApiOAuthFlows
			{
				AuthorizationCode = new OpenApiOAuthFlow
				{
					AuthorizationUrl = new Uri(keycloak.AuthorizationUrl),
					TokenUrl = new Uri(keycloak.TokenUrl),
					Scopes = ParseScopes(keycloak.Scopes)
				}
			}
		});

		return options;
	}

	private static Dictionary<string, string> ParseScopes(string scopes)
	{
		return scopes
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.ToDictionary(s => s, s => s);
	}
	#endregion
}
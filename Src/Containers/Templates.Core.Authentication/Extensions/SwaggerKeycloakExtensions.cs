using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.SwaggerGen;
using Templates.Core.Authentication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Templates.Core.Authentication.Extensions;

/// <summary>
/// Extension methods to configure Swagger UI with Keycloak OAuth2 PKCE flow.
///
/// Usage in Program.cs:
/// <code>
///   builder.Services.AddSwaggerGen(c =>
///   {
///       c.AddKeycloakSecurityDefinition(builder.Services.BuildServiceProvider());
///       c.AddKeycloakSecurityRequirement();
///   });
///
///   // In the app pipeline:
///   app.UseSwaggerUI(c => c.UseKeycloakOAuth(app.Services));
/// </code>
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
		options.AddSecurityRequirement(document =>
			new OpenApiSecurityRequirement
			{
				// For non-OAuth2 schemes, scopes MUST be empty
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

		// Set via OAuthConfigObject — works across all Swashbuckle 6.x / 7.x versions
		options.OAuthConfigObject.ScopeSeparator = " ";
		options.OAuthConfigObject.ClientId = keycloak.ClientId;
		options.OAuthConfigObject.UsePkceWithAuthorizationCodeGrant = true;
		options.OAuthConfigObject.Scopes = keycloak.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		return options;
	}

	/// <summary>
	/// Adds the Keycloak OAuth2 Authorization Code + PKCE security definition to Swagger.
	/// </summary>
	public static SwaggerGenOptions AddKeycloakSecurityDefinition(this SwaggerGenOptions options, IServiceProvider serviceProvider)
	{
		var keycloak = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

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
					Scopes = ParseScopes(keycloak.Scopes),
				}
			}
		});

		return options;
	}
	#endregion

	#region Helpers
	private static Dictionary<string, string> ParseScopes(string scopes)
	{
		return scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToDictionary(s => s, s => s);
	}
	#endregion
}
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Pivot.Framework.Authentication.AspNetCore.Helpers;

namespace Pivot.Framework.Authentication.AspNetCore.Extensions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers Keycloak JWT Bearer authentication for the ASP.NET Core backend.
///              Loads and validates Keycloak options, configures JWT bearer validation,
///              flattens Keycloak roles into standard .NET role claims, and wires auth logging.
/// </summary>
public static class KeycloakAuthenticationExtensions
{
	#region Public Methods
	/// <summary>
	/// Registers Keycloak JWT bearer authentication.
	/// </summary>
	public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration, Action<JwtBearerOptions>? configureOptions = null)
	{
		var options = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? throw new InvalidOperationException($"Missing configuration section '{KeycloakOptions.SectionName}'.");

		options.Validate();

		services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
			{
				jwt.Authority = options.IssuerUrl;
				jwt.Audience = options.Audience;
				jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
				jwt.MetadataAddress = options.MetadataUrl;

				jwt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = options.IssuerUrl,

					ValidateAudience = true,
					ValidAudience = options.Audience,

					ValidateLifetime = true,
					ClockSkew = TimeSpan.FromSeconds(30),

					// Use the standard .NET role claim type so [Authorize(Roles = "...")] and
					// KeycloakClaimsTransformer.FlattenRoles both operate on the same claim name.
					RoleClaimType = ClaimTypes.Role,
					NameClaimType = "preferred_username"
				};

				jwt.Events = new JwtBearerEvents
				{
					OnTokenValidated = ctx =>
					{
						KeycloakClaimsTransformer.FlattenRoles(ctx);
						return Task.CompletedTask;
					},
					OnAuthenticationFailed = ctx =>
					{
						var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
						logger.LogWarning(ctx.Exception, "Keycloak authentication failed: {Message}", ctx.Exception.Message);
						return Task.CompletedTask;
					}
				};

				configureOptions?.Invoke(jwt);
			});

		services.AddAuthorization();

		return services;
	}
	#endregion
}
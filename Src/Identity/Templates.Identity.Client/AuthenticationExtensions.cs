using Templates.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Templates.Identity.Client;

public static class AuthenticationExtensions
{
	public static AuthenticationBuilder AddAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddTransient<ITicketStore, InMemoryTicketStore>();
		services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, ConfigureCookieAuthenticationOptions>();
		JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
		var builder = services.AddAuthentication(options =>
		{
			options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
		})
			  .AddCookie()
			  .AddBearerToken(IdentityConstants.BearerScheme)
			  .AddOpenIdConnect(options =>
			  {
				  options.Scope.Clear();
				  configuration.Bind("oidc", options);
				  options.RequireHttpsMetadata = false;
				  options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				  options.ResponseType = OpenIdConnectResponseType.Code;

				  options.SaveTokens = true;
				  options.GetClaimsFromUserInfoEndpoint = true;
				  options.MapInboundClaims = false;
				  options.TokenValidationParameters = new TokenValidationParameters
				  {
					  NameClaimType = "name",
					  RoleClaimType = "role"
				  };
				  options.Events = new OpenIdConnectEvents
				  {
					  OnAccessDenied = delegate (AccessDeniedContext context)
					  {
						  context.HandleResponse();
						  context.Response.Redirect("/");
						  return Task.CompletedTask;
					  }
				  };
			  });

		return builder;
	}

	public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		var apiConfig = new ApiAuthentication();
		configuration.Bind("ApiAuthentication", apiConfig);
		services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
				.AddOAuth2Introspection(options =>
				{
					options.Authority = apiConfig.Authority;
					options.ClientId = apiConfig.ApiName;
					options.ClientSecret = apiConfig.ApiSecret;
				});

		return services;
	}
}
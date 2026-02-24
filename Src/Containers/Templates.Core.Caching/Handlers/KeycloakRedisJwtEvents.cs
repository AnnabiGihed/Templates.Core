using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Templates.Core.Caching.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Templates.Core.Caching.Handlers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Plugs Redis token caching and revocation checking into the Keycloak JWT bearer pipeline.
/// </summary>
public sealed class KeycloakRedisJwtEvents : JwtBearerEvents
{
	#region Dependencies
	private readonly IDistributedTokenCache _tokenCache;
	private readonly ITokenRevocationCache _revocationCache;
	private readonly ILogger<KeycloakRedisJwtEvents> _logger;
	#endregion

	#region Constructor
	public KeycloakRedisJwtEvents(IDistributedTokenCache tokenCache, ITokenRevocationCache revocationCache, ILogger<KeycloakRedisJwtEvents> logger)
	{
		_logger = logger;
		_tokenCache = tokenCache;
		_revocationCache = revocationCache;
	}
	#endregion

	#region Overrides
	public override async Task TokenValidated(TokenValidatedContext context)
	{
		var accessToken = context.SecurityToken is JsonWebToken jwt
			? jwt.EncodedToken
			: context.Request.Headers.Authorization
				.ToString()
				.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
				.Trim();

		if (string.IsNullOrEmpty(accessToken)) 
			return;

		var (subjectStr, issuedAt, expiresAt, rawClaims) = ExtractTokenInfo(context.SecurityToken);

		#region 1. Individual token revocation check
		if (await _revocationCache.IsRevokedAsync(accessToken, context.HttpContext.RequestAborted))
		{
			_logger.LogWarning("Rejected revoked token for sub={Sub}", subjectStr ?? "unknown");
			context.Fail("Token has been revoked.");
			return;
		}
		#endregion

		#region 2. Global user revocation check
		if (subjectStr is not null && issuedAt is not null && Guid.TryParse(subjectStr, out var subjectGuid))
		{
			if (await _revocationCache.IsIssuedBeforeRevocationAsync(
					subjectGuid, issuedAt.Value, context.HttpContext.RequestAborted))
			{
				_logger.LogWarning("Rejected globally-revoked token for user {Sub}", subjectStr);
				context.Fail("All tokens for this user have been revoked.");
				return;
			}
		}
		#endregion

		#region 3. Claims cache fast path
		var cached = await _tokenCache.GetClaimsAsync(accessToken, context.HttpContext.RequestAborted);

		if (cached is not null)
		{
			var claims = RebuildClaims(cached);
			var identity = new ClaimsIdentity(claims, "keycloak", "preferred_username", ClaimTypes.Role);

			context.Principal = new ClaimsPrincipal(identity);
			_logger.LogDebug("Token claims served from Redis cache.");
			return;
		}
		#endregion

		#region 4. First request — flatten Keycloak roles and cache claims
		if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity)
			return;

		FlattenKeycloakRoles(claimsIdentity);

		if (expiresAt is not null && subjectStr is not null)
		{
			var allClaims = (rawClaims ?? claimsIdentity.Claims)
				.GroupBy(c => c.Type)
				.ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToList());

			var claimsToCache = new CachedTokenClaims
			{
				UserId = subjectStr,
				Username = claimsIdentity.FindFirst("preferred_username")?.Value ?? string.Empty,
				Roles = claimsIdentity.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
				Email = claimsIdentity.FindFirst("email")?.Value ?? claimsIdentity.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
				AllClaims = allClaims
			};

			await _tokenCache.SetClaimsAsync(accessToken, claimsToCache, expiresAt.Value, context.HttpContext.RequestAborted);

			_logger.LogDebug("Token claims for user {UserId} written to Redis. Roles: [{Roles}]",
				subjectStr, string.Join(", ", claimsToCache.Roles));
		}
		else
		{
			_logger.LogDebug("Token metadata unavailable — claims not cached. Role flattening still applied.");
		}
		#endregion
	}
	#endregion

	#region Token info extraction
	/// <summary>
	/// Extracts subject, issuedAt, expiresAt and raw claims from a
	/// <see cref="JsonWebToken"/> (.NET 8+ default from JsonWebTokenHandler).
	/// </summary>
	private static (string? subject, DateTimeOffset? issuedAt, DateTimeOffset? expiresAt, IEnumerable<Claim>? rawClaims)
		ExtractTokenInfo(Microsoft.IdentityModel.Tokens.SecurityToken? securityToken)
	{
		if (securityToken is not JsonWebToken jwt)
			return (null, null, null, null);

		DateTimeOffset? iat = jwt.IssuedAt == DateTime.MinValue ? null : new DateTimeOffset(jwt.IssuedAt, TimeSpan.Zero);

		DateTimeOffset? exp = jwt.ValidTo == DateTime.MinValue ? null : new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);

		var claims = new List<Claim>();
		try
		{
			foreach (var claim in jwt.Claims)
				claims.Add(claim);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"KeycloakRedisJwtEvents: failed to enumerate claims: {ex.Message}");
		}

		return (jwt.Subject, iat, exp, claims);
	}
	#endregion

	#region Keycloak role flattening
	private static void FlattenKeycloakRoles(ClaimsIdentity identity)
	{
		var realmClaim = identity.FindFirst("realm_access");
		if (realmClaim is not null)
		{
			try
			{
				using var doc = JsonDocument.Parse(realmClaim.Value);
				if (doc.RootElement.TryGetProperty("roles", out var roles))
					foreach (var r in roles.EnumerateArray())
						if (r.GetString() is { } rv)
							identity.AddClaim(new Claim(ClaimTypes.Role, rv));
			}
			catch (JsonException) { }
		}

		var resourceClaim = identity.FindFirst("resource_access");
		if (resourceClaim is not null)
		{
			try
			{
				using var doc = JsonDocument.Parse(resourceClaim.Value);
				foreach (var client in doc.RootElement.EnumerateObject())
					if (client.Value.TryGetProperty("roles", out var roles))
						foreach (var r in roles.EnumerateArray())
							if (r.GetString() is { } rv)
								identity.AddClaim(new Claim(ClaimTypes.Role, rv));
			}
			catch (JsonException) { }
		}
	}
	#endregion

	#region Claims rebuild from cache
	/// <summary>
	/// Rebuilds a <see cref="Claim"/> list from a cached snapshot.
	/// Emits all values for multi-value claim types so no data is lost.
	/// </summary>
	private static List<Claim> RebuildClaims(CachedTokenClaims cached)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, cached.UserId),
			new("sub",                     cached.UserId),
			new("preferred_username",      cached.Username),
			new(ClaimTypes.Email,          cached.Email),
		};

		claims.AddRange(cached.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

		var alreadyEmitted = new HashSet<string>
		{
			ClaimTypes.NameIdentifier, "sub", "preferred_username", ClaimTypes.Email
		};

		foreach (var (type, values) in cached.AllClaims)
		{
			if (alreadyEmitted.Contains(type)) continue;

			foreach (var value in values)
				claims.Add(new Claim(type, value));
		}

		return claims;
	}
	#endregion
}
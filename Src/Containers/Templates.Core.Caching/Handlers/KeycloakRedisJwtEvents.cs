using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Templates.Core.Caching.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Templates.Core.Caching.Handlers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Plugs Redis token caching and revocation checking into the Keycloak JWT bearer pipeline.
///
/// On <c>OnTokenValidated</c>:
///  1. Checks the individual-token revocation blacklist.
///  2. Checks the global user revocation timestamp ("logout everywhere").
///  3. Serves claims from Redis cache if available (fast path — skips re-parsing).
///  4. On first request: flattens Keycloak roles into ClaimTypes.Role and writes to Redis.
///
/// .NET 10 TOKEN HANDLER NOTE:
/// ─────────────────────────────────────────────────────────────────────────────
/// AddJwtBearer in .NET 8+ defaults to <see cref="Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler"/>
/// which produces <see cref="JsonWebToken"/>, NOT <see cref="JwtSecurityToken"/>.
/// Both types are handled via <see cref="ExtractTokenInfo"/> so this class works
/// regardless of which handler is active.
/// </summary>
public sealed class KeycloakRedisJwtEvents : JwtBearerEvents
{
	#region Dependencies
	private readonly IDistributedTokenCache _tokenCache;
	private readonly ITokenRevocationCache _revocationCache;
	private readonly ILogger<KeycloakRedisJwtEvents> _logger;
	#endregion

	#region Constructor
	public KeycloakRedisJwtEvents(
		IDistributedTokenCache tokenCache,
		ITokenRevocationCache revocationCache,
		ILogger<KeycloakRedisJwtEvents> logger)
	{
		_tokenCache = tokenCache;
		_revocationCache = revocationCache;
		_logger = logger;
	}
	#endregion

	#region Overrides
	public override async Task TokenValidated(TokenValidatedContext context)
	{
		var accessToken = context.Request.Headers.Authorization
			.ToString()
			.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
			.Trim();

		if (string.IsNullOrEmpty(accessToken)) return;

		// Handles both JsonWebToken (.NET 8+ default) and JwtSecurityToken (explicit override)
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
		// ICurrentUser.UserId is Guid? — Keycloak sub is always a UUID, so this parse always succeeds.
		if (subjectStr is not null && issuedAt is not null
			&& Guid.TryParse(subjectStr, out var subjectGuid))
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

		// Flatten realm_access.roles and resource_access.<clientId>.roles → ClaimTypes.Role
		FlattenKeycloakRoles(claimsIdentity);

		if (expiresAt is not null && subjectStr is not null)
		{
			var claimsToCache = new CachedTokenClaims
			{
				// CachedTokenClaims.UserId is string — it's a Redis DTO.
				// ICurrentUser.UserId parses it to Guid on access.
				UserId = subjectStr,
				Email = claimsIdentity.FindFirst("email")?.Value
						   ?? claimsIdentity.FindFirst(ClaimTypes.Email)?.Value
						   ?? string.Empty,
				Username = claimsIdentity.FindFirst("preferred_username")?.Value ?? string.Empty,
				Roles = claimsIdentity.Claims
					.Where(c => c.Type == ClaimTypes.Role)
					.Select(c => c.Value)
					.ToList(),
				AllClaims = rawClaims?.GroupBy(c => c.Type)
									  .ToDictionary(g => g.Key, g => g.First().Value)
						?? claimsIdentity.Claims
										 .GroupBy(c => c.Type)
										 .ToDictionary(g => g.Key, g => g.First().Value),
			};

			await _tokenCache.SetClaimsAsync(
				accessToken, claimsToCache, expiresAt.Value,
				context.HttpContext.RequestAborted);

			_logger.LogDebug(
				"Token claims for user {UserId} written to Redis cache. Roles: [{Roles}]",
				subjectStr,
				string.Join(", ", claimsToCache.Roles));
		}
		else
		{
			_logger.LogDebug(
				"Token metadata unavailable — claims not cached. Role flattening still applied.");
		}
		#endregion
	}
	#endregion

	#region Token info extraction (handles both JwtSecurityToken and JsonWebToken)
	/// <summary>
	/// Extracts subject, issuedAt, expiresAt and raw claims from either
	/// <see cref="JwtSecurityToken"/> (.NET 6/7, or explicit handler override) or
	/// <see cref="JsonWebToken"/> (.NET 8+ default from JsonWebTokenHandler).
	/// Returns nulls for metadata if the token type is unknown.
	/// </summary>
	private static (string? subject, DateTimeOffset? issuedAt, DateTimeOffset? expiresAt, IEnumerable<Claim>? rawClaims)
		ExtractTokenInfo(Microsoft.IdentityModel.Tokens.SecurityToken? securityToken)
	{
		// .NET 6/7 / explicit JwtSecurityTokenHandler
		if (securityToken is JwtSecurityToken jst)
		{
			DateTimeOffset? iat = null;
			var iatClaim = jst.Claims.FirstOrDefault(c => c.Type == "iat");
			if (iatClaim is not null && long.TryParse(iatClaim.Value, out var iatUnix))
				iat = DateTimeOffset.FromUnixTimeSeconds(iatUnix);

			var expClaim = jst.Claims.FirstOrDefault(c => c.Type == "exp");
			DateTimeOffset? exp = expClaim is not null && long.TryParse(expClaim.Value, out var expUnix)
				? DateTimeOffset.FromUnixTimeSeconds(expUnix)
				: null;

			return (jst.Subject, iat, exp, jst.Claims);
		}

		// .NET 8+ JsonWebTokenHandler (default in .NET 10)
		if (securityToken is JsonWebToken jwt)
		{
			DateTimeOffset? iat = jwt.IssuedAt == DateTime.MinValue
				? null
				: new DateTimeOffset(jwt.IssuedAt, TimeSpan.Zero);

			DateTimeOffset? exp = jwt.ValidTo == DateTime.MinValue
				? null
				: new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);

			// JsonWebToken doesn't expose Claims directly — build from the raw JSON payload
			var claims = new List<Claim>();
			try
			{
				using var doc = JsonDocument.Parse(jwt.EncodedPayload
					.Replace('-', '+').Replace('_', '/')
					.PadRight(jwt.EncodedPayload.Length + (4 - jwt.EncodedPayload.Length % 4) % 4, '=') is var padded
					? Convert.FromBase64String(padded) is var bytes
						? System.Text.Encoding.UTF8.GetString(bytes)
						: "{}"
					: "{}");

				foreach (var property in doc.RootElement.EnumerateObject())
					claims.Add(new Claim(property.Name, property.Value.ToString()));
			}
			catch { /* malformed payload — return empty claims */ }

			return (jwt.Subject, iat, exp, claims);
		}

		return (null, null, null, null);
	}
	#endregion

	#region Keycloak role flattening
	private static void FlattenKeycloakRoles(ClaimsIdentity identity)
	{
		// realm_access.roles → ClaimTypes.Role
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
			catch { /* malformed — skip */ }
		}

		// resource_access.<clientId>.roles → ClaimTypes.Role
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
			catch { /* malformed — skip */ }
		}
	}
	#endregion

	#region Claims rebuild from cache
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

		foreach (var (key, value) in cached.AllClaims)
			if (!claims.Exists(c => c.Type == key))
				claims.Add(new Claim(key, value));

		return claims;
	}
	#endregion
}
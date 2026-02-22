using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Templates.Core.Caching.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Templates.Core.Caching.Handlers;

/// <summary>
/// Plugs Redis token caching and revocation checking into the Keycloak JWT bearer pipeline.
///
/// On <c>OnTokenValidated</c>:
///  1. Checks the revocation blacklist — rejects revoked tokens immediately.
///  2. Checks the claims cache — if found, replaces parsed claims with cached ones (fast path).
///  3. If not cached, flattens Keycloak roles into ClaimTypes.Role and stores them in Redis.
///
/// .NET 10 TOKEN HANDLER BUG — WHY BOTH TYPES ARE CHECKED:
/// ─────────────────────────────────────────────────────────────────────────────
/// In .NET 8+, AddJwtBearer uses JsonWebTokenHandler by default (not JwtSecurityTokenHandler).
/// JsonWebTokenHandler produces Microsoft.IdentityModel.JsonWebTokens.JsonWebToken,
/// not System.IdentityModel.Tokens.Jwt.JwtSecurityToken.
///
/// The original code did:
///     var jwt = context.SecurityToken as JwtSecurityToken;
///
/// On .NET 10 this returns null. Region 4's guard "jwt is not null" then fails,
/// so FlattenKeycloakRoles is never called and no claims are ever cached.
/// Every request returned a principal without any ClaimTypes.Role claims → 403.
///
/// Fix: try both casts. Extract claims via a unified helper that works with either type.
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
		_logger = logger;
		_tokenCache = tokenCache;
		_revocationCache = revocationCache;
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

		// ── Resolve the security token — works with both token handlers ──────────
		// .NET 8+ AddJwtBearer defaults to JsonWebTokenHandler → JsonWebToken.
		// .NET 6/7 / explicit override uses JwtSecurityTokenHandler → JwtSecurityToken.
		var (subject, issuedAt, expiresAt, rawClaims) = ExtractTokenInfo(context.SecurityToken);

		#region 1. Revocation check (individual token)
		if (await _revocationCache.IsRevokedAsync(accessToken, context.HttpContext.RequestAborted))
		{
			_logger.LogWarning("Rejected revoked token for sub={Sub}", subject ?? "unknown");
			context.Fail("Token has been revoked.");
			return;
		}
		#endregion

		#region 2. Global user revocation check
		if (subject is not null && issuedAt is not null)
		{
			if (await _revocationCache.IsIssuedBeforeRevocationAsync(
					subject, issuedAt.Value, context.HttpContext.RequestAborted))
			{
				_logger.LogWarning("Rejected globally-revoked token for user {Sub}", subject);
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

		#region 4. First request — flatten roles, extract and cache claims
		if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity)
			return;

		// Flatten Keycloak realm + client roles into standard ClaimTypes.Role claims.
		// This is always done regardless of whether we have a full token object,
		// because the identity contains the parsed claims from the JWT payload.
		FlattenKeycloakRoles(claimsIdentity);

		// Only cache if we could extract token metadata (expiry is required for TTL).
		if (expiresAt is not null && subject is not null)
		{
			var claimsToCache = new CachedTokenClaims
			{
				UserId = subject,
				Email = claimsIdentity.FindFirst("email")?.Value
						   ?? claimsIdentity.FindFirst(ClaimTypes.Email)?.Value
						   ?? string.Empty,
				Username = claimsIdentity.FindFirst("preferred_username")?.Value ?? string.Empty,

				// Roles were just added by FlattenKeycloakRoles above.
				Roles = claimsIdentity.Claims
					.Where(c => c.Type == ClaimTypes.Role)
					.Select(c => c.Value)
					.ToList(),

				// AllClaims: raw claim snapshot for the Redis rebuild path.
				// Use the pre-parsed rawClaims if available; fall back to identity claims.
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
				subject,
				string.Join(", ", claimsToCache.Roles));
		}
		else
		{
			_logger.LogDebug(
				"Token metadata unavailable — claims not cached. " +
				"Role flattening still applied for this request.");
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

			var exp = DateTimeOffset.FromUnixTimeSeconds(
				long.Parse(jst.Claims.First(c => c.Type == "exp").Value));

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

			// JsonWebToken.Claims exposes IEnumerable<Claim> from the payload
			return (jwt.Subject, iat, exp, jwt.Claims);
		}

		return (null, null, null, null);
	}
	#endregion

	#region Helpers
	private static void FlattenKeycloakRoles(ClaimsIdentity identity)
	{
		// ── Realm roles ────────────────────────────────────────────────────────
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

		// ── Client / resource roles ────────────────────────────────────────────
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

	private static List<Claim> RebuildClaims(CachedTokenClaims cached)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, cached.UserId),
			new("preferred_username",       cached.Username),
			new(ClaimTypes.Email,           cached.Email),
		};

		claims.AddRange(cached.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

		foreach (var (key, value) in cached.AllClaims)
			if (!claims.Exists(c => c.Type == key))
				claims.Add(new Claim(key, value));

		return claims;
	}
	#endregion
}
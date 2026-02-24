/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Configuration options for <see cref="ITokenRevocationCache"/>.
///              Bind from appsettings.json under "TokenRevocation".
/// </summary>
public sealed class TokenRevocationOptions
{
	public const string SectionName = "TokenRevocation";

	/// <summary>
	/// How long a "revoke-all-for-user" sentinel is kept in Redis.
	/// Set this to your Keycloak realm's maximum refresh token lifetime
	/// (SSO Session Max in Realm Settings > Tokens).
	/// Defaults to 30 days which covers most standard Keycloak configurations.
	/// </summary>
	public TimeSpan RevokeAllTtl { get; set; } = TimeSpan.FromDays(30);
}
namespace Pivot.Framework.Authentication.Caching.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Configuration options for <see cref="ITokenRevocationCache"/>.
///              Bind from appsettings.json under "TokenRevocation".
/// </summary>
public sealed class TokenRevocationOptions
{
	/// <summary>
	/// The appsettings.json section name for this options block.
	/// </summary>
	public const string SectionName = "TokenRevocation";

	/// <summary>
	/// How long a "revoke-all-for-user" sentinel is kept in Redis.
	/// Set this to your Keycloak realm's maximum refresh token lifetime
	/// (SSO Session Max in Realm Settings > Tokens).
	/// Defaults to 30 days which covers most standard Keycloak configurations.
	/// </summary>
	public TimeSpan RevokeAllTtl { get; set; } = TimeSpan.FromDays(30);

	/// <summary>
	/// Validates that the options are in a usable state.
	/// Throws <see cref="InvalidOperationException"/> if <see cref="RevokeAllTtl"/> is
	/// zero or negative, which would make global user revocation silently ineffective.
	/// </summary>
	public void Validate()
	{
		if (RevokeAllTtl <= TimeSpan.Zero)
			throw new InvalidOperationException($"{SectionName}.{nameof(RevokeAllTtl)} must be a positive duration. " + $"Set it to your Keycloak realm's SSO Session Max (e.g. '30.00:00:00' for 30 days).");
	}
}
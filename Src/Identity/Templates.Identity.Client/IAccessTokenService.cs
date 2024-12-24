namespace Templates.Identity.Client;

public interface IAccessTokenService
{
	Task<DateTimeOffset> GetExpirationTime();
}

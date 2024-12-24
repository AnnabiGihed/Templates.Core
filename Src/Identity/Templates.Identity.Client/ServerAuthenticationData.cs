namespace Templates.Identity.Client;

public class ServerAuthenticationData
{
	public string SubjectId { get; set; }
	public DateTimeOffset Expiration { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
	public DateTimeOffset ExpireAt { get; set; }
	public string IdToken { get; set; }
	public DateTimeOffset RefreshAt { get; set; }
}

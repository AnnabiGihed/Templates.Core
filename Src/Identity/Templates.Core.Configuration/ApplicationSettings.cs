namespace Templates.Core.Configuration;

public class ApplicationSettings
{
	public Dictionary<string, string> WebApis { get; set; }

	public Dictionary<string, WebserviceSettings> WebServices { get; set; }

	public Email Email { get; set; }

	public ApiAuthentication ApiAuthentication { get; set; }

	public Dictionary<string, ConnectionStringSettings> ConnectionStrings { get; set; }

}
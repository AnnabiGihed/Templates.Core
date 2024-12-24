using Microsoft.Extensions.Configuration;

namespace Templates.Core.Configuration;

public static class Extensions
{
	public static ConnectionStringSettings GetConnectionStringSettings(this IConfiguration configuration, string name, string section = "ConnectionStrings")
	{
		var connectionStringCollection = configuration.GetSection(section).Get<Dictionary<string, ConnectionStringSettings>>();
		if (connectionStringCollection == null ||
			!connectionStringCollection.TryGetValue(name, out ConnectionStringSettings connectionStringSettings))
		{
			return null;
		}

		return connectionStringSettings;
	}

	public static string GetWebApiUrl(this IConfiguration configuration, string name, string section = "WebApis")
	{
		var webApis = configuration.GetSection(section).Get<Dictionary<string, string>>();
		if (webApis == null || !webApis.TryGetValue(name, out string url))
		{
			return null;
		}

		return url;
	}

	public static WebserviceSettings GetWebService(this IConfiguration configuration, string name, string section = "WebServices")
	{
		var webServices = configuration.GetSection(section).Get<Dictionary<string, WebserviceSettings>>();
		if (webServices == null || !webServices.TryGetValue(name, out WebserviceSettings ws))
		{
			return null;
		}

		return ws;
	}
}

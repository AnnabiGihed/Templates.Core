using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Templates.Identity.Client;

public class ConfigureCookieAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
	private readonly ITicketStore _ticketStore;

	public ConfigureCookieAuthenticationOptions(ITicketStore ticketStore)
	{
		_ticketStore = ticketStore;
	}

	public void PostConfigure(string name, CookieAuthenticationOptions options)
	{
		options.SessionStore = _ticketStore;
	}
}
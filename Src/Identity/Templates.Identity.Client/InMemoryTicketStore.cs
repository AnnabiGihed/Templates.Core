using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Templates.Identity.Client;

public class InMemoryTicketStore : ITicketStore
{
	private readonly IMemoryCache _cache;

	public InMemoryTicketStore(IMemoryCache cache)
	{
		_cache = cache;
	}

	public Task RemoveAsync(string key)
	{
		_cache.Remove(key);

		return Task.CompletedTask;
	}

	public Task<AuthenticationTicket> RetrieveAsync(string key)
	{
		var ticket = _cache.Get<AuthenticationTicket>(key);

		return Task.FromResult(ticket);
	}

	public Task RenewAsync(string key, AuthenticationTicket ticket)
	{
		_cache.Set(key, ticket);

		return Task.CompletedTask;
	}

	public Task<string> StoreAsync(AuthenticationTicket ticket)
	{
		var key = Guid.NewGuid().ToString();
		_cache.Set(key, ticket);

		return Task.FromResult(key);
	}
}

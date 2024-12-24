using System.Collections.Concurrent;

namespace Templates.Identity.Client;

public class ServerAuthenticationStateCache
{
	private readonly ConcurrentDictionary<string, ServerAuthenticationData> Cache = new ConcurrentDictionary<string, ServerAuthenticationData>();

	public bool HasSubjectId(string subjectId) => Cache.ContainsKey(subjectId);

	public void Add(string subjectId, DateTimeOffset expiration, string idToken, string accessToken, string refreshToken, DateTimeOffset refreshAt)
	{
		var data = new ServerAuthenticationData
		{
			SubjectId = subjectId,
			Expiration = expiration,
			IdToken = idToken,
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			RefreshAt = refreshAt
		};
		Cache.AddOrUpdate(subjectId, data, (k, v) => data);
	}

	public ServerAuthenticationData Get(string subjectId)
	{
		Cache.TryGetValue(subjectId, out var data);
		return data;
	}

	public void Remove(string subjectId)
	{
		Cache.TryRemove(subjectId, out _);
	}
}

namespace Templates.Core.Configuration;

public class ConnectionStringSettings
{
	public string ConnectionString { get; set; }

	public string ProviderName { get; set; }

	public bool IsOracle => ProviderNameContains("Oracle");

	public bool IsSqlServer => ProviderNameContains("SqlClient");

	bool ProviderNameContains(string pattern) => !String.IsNullOrEmpty(ProviderName) && ProviderName.ToLower().Contains(pattern.ToLower());
}

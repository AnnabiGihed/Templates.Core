namespace Templates.Core.Configuration;

public class Email
{
	public string Server { get; set; }

	public int Port { get; set; }

	public string PickupDirectoryLocation { get; set; }

	public string From { get; set; }

	public string Migrations { get; set; }
}

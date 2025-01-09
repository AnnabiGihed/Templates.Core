namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.RabbitMQ.Models;

public class RabbitMQSettings
{
	public required int Port { get; set; }
	public required string Queue { get; set; }
	public required string HostName { get; set; }
	public required string UserName { get; set; }
	public required string Password { get; set; }
	public required string Exchange { get; set; }
	public required string RoutingKey { get; set; }
	public required string VirtualHost { get; set; }
	public required string EncryptionKey { get; set; }
	public required string ClientProvidedName { get; set; }
}
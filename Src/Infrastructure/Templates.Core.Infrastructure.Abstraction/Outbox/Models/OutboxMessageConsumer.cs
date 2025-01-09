namespace Templates.Core.Infrastructure.Abstraction.Outbox.Models;

public sealed class OutboxMessageConsumer
{
	public Guid Id { get; set; }

	public string Name { get; set; } = string.Empty;
}

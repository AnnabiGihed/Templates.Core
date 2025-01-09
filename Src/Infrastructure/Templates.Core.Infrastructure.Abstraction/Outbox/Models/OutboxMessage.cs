namespace Templates.Core.Infrastructure.Abstraction.Outbox.Models;

public sealed class OutboxMessage
{
	public Guid Id { get; set; }
	public string? Payload { get; set; }
	public string? EventType { get; set; }
	public int RetryCount { get; set; } = 0;
	public DateTime CreatedAtUtc { get; set; }
	public bool Processed { get; set; } = false;
	public DateTime? ProcessedAtUtc { get; set; }
}
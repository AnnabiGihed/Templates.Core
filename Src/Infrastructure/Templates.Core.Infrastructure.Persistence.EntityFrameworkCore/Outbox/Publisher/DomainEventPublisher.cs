using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Templates.Core.Infrastructure.Abstraction.Outbox.DomainEventPublisher;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Persists domain events into the Outbox as serialized messages.
///              This component does NOT publish to the broker directly.
///              Publishing is handled by the outbox processor after transaction commit.
/// </summary>
/// <typeparam name="TContext">EF Core DbContext type that stores the outbox table.</typeparam>
public sealed class DomainEventPublisher<TContext> : IDomainEventPublisher
	where TContext : DbContext
{
	private static readonly JsonSerializerSettings SerializerSettings = new()
	{
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		Formatting = Formatting.None,
		StringEscapeHandling = StringEscapeHandling.Default
	};

	private readonly IOutboxRepository<TContext> _outboxRepository;
	private readonly ILogger<DomainEventPublisher<TContext>> _logger;

	public DomainEventPublisher(
		IOutboxRepository<TContext> outboxRepository,
		ILogger<DomainEventPublisher<TContext>> logger)
	{
		_outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Serializes a domain event and stores it in the outbox within the current transaction.
	/// </summary>
	public async Task<Result> PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(domainEvent);

			var eventType = domainEvent.GetType().AssemblyQualifiedName;
			if (string.IsNullOrWhiteSpace(eventType))
				return Result.Failure(new Error("DomainEventTypeError", "Domain event type name could not be resolved."));

			var payload = JsonConvert.SerializeObject(domainEvent, SerializerSettings);

			var outboxMessage = new OutboxMessage
			{
				// IMPORTANT: This assumes domainEvent.Id is globally unique per event instance.
				// If you cannot guarantee that, switch to Guid.NewGuid() and store domainEvent.Id separately.
				Id = domainEvent.Id,
				Payload = payload,
				CreatedAtUtc = domainEvent.OccurredOnUtc,
				EventType = eventType
			};

			_logger.LogDebug("Enqueueing domain event to outbox: {EventType} ({EventId})", eventType, domainEvent.Id);

			return await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while enqueueing domain event to outbox.");
			return Result.Failure(new Error("DomainEventPublishError", $"Error while publishing domain event: {ex.Message}"));
		}
	}
}

using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;
using Templates.Core.Infrastructure.Abstraction.Outbox.DomainEventPublisher;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;

public class DomainEventPublisher<TContext> : IDomainEventPublisher where TContext : DbContext
{
	protected readonly IOutboxRepository<TContext> _outboxRepository;

	public DomainEventPublisher(IOutboxRepository<TContext> outboxRepository)
	{
		_outboxRepository = outboxRepository;
	}

	public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(domainEvent);

			var serializedObject = JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Prevents circular references
				Formatting = Formatting.None, // Compact JSON
				StringEscapeHandling = StringEscapeHandling.Default // Avoids unnecessary escaping
			});

			var outboxMessage = new OutboxMessage
			{
				Id = domainEvent.Id,
				EventType = domainEvent?.GetType()?.AssemblyQualifiedName,
				Payload = serializedObject,
				CreatedAtUtc = domainEvent.OccurredOnUtc
			};

			await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error while publishing domain event: {ex.Message}");
		}
	}
}

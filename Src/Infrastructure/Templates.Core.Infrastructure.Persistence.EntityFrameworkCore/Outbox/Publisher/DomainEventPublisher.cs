using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using Templates.Core.Domain.Primitives;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;

public class DomainEventPublisher<TContext> : IDomainEventPublisher where TContext : DbContext
{
	private readonly IOutboxRepository<TContext> _outboxRepository;

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
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
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

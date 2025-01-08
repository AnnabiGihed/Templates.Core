using Newtonsoft.Json;
using Templates.Core.Domain.Primitives;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;

public class DomainEventPublisher : IDomainEventPublisher
{
	private readonly IOutboxRepository _outboxRepository;

	public DomainEventPublisher(IOutboxRepository outboxRepository)
	{
		_outboxRepository = outboxRepository;
	}

	public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
	{
		var serializedObject = JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		});

		var outboxMessage = new OutboxMessage
		{
			Id = domainEvent.Id,
			EventType = domainEvent.GetType().FullName,
			Payload = serializedObject,
			CreatedAtUtc = domainEvent.OccurredOnUtc
		};

		await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
	}
}
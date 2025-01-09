using Templates.Core.Domain.Primitives;

namespace Templates.Core.Infrastructure.Abstraction.Outbox.DomainEventPublisher;

public interface IDomainEventPublisher
{
	Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

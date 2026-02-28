using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;

namespace Pivot.Framework.Infrastructure.Abstraction.Outbox.DomainEventPublisher;

public interface IDomainEventPublisher
{
	Task<Result> PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

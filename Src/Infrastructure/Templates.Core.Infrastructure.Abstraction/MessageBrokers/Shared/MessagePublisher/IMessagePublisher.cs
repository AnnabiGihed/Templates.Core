using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;

namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

public interface IMessagePublisher : IDisposable
{
	Task<Result> PublishAsync(OutboxMessage message);
}
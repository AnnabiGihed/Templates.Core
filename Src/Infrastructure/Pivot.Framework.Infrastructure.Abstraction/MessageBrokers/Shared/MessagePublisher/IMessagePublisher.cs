using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.Models;

namespace Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

public interface IMessagePublisher : IDisposable
{
	Task<Result> PublishAsync(OutboxMessage message);
}
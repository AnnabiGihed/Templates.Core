using Templates.Core.Domain.Shared;

namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

public interface IMessagePublisher : IDisposable
{
	Task<Result> PublishAsync<T>(T message);
}
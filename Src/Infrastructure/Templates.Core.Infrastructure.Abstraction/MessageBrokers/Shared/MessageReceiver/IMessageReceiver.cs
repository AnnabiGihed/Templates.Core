namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageReceiver;

public interface IMessageReceiver : IDisposable
{
	Task InitializeAsync();
	Task StartListeningAsync();
}

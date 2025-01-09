namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageSerializer;

public interface IMessageSerializer
{
	byte[] Serialize<T>(T message);
}


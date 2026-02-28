namespace Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageSerializer;

public interface IMessageSerializer
{
	byte[] Serialize<T>(T message);
}


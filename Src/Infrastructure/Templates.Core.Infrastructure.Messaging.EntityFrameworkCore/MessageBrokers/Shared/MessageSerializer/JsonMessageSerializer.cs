using System.Text;
using System.Text.Json;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageSerializer;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageSerializer;

public class JsonMessageSerializer : IMessageSerializer
{
	#region IMessageSerializer Implementation
	public byte[] Serialize<T>(T message)
	{
		if (message == null) throw new ArgumentNullException(nameof(message));
		return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
	}
	#endregion
}

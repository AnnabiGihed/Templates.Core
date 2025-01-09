using System.Text;
using Polly.Retry;
using RabbitMQ.Client;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Options;
using Templates.Core.Domain.Shared;
using System.Collections.Concurrent;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.RabbitMQ.Models;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageCompressor;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageSerializer;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessagePublisher;

public class RabbitMQPublisher(IOptions<RabbitMQSettings> options, IMessageSerializer serializer, IMessageCompressor compressor, IMessageEncryptor encryptor, AsyncRetryPolicy retryPolicy, AsyncCircuitBreakerPolicy circuitBreaker) : IMessagePublisher
{
	#region Properties
	protected readonly RabbitMQSettings _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
	protected readonly IMessageEncryptor _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
	protected readonly AsyncRetryPolicy _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
	protected readonly IMessageCompressor _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
	protected readonly IMessageSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	protected readonly AsyncCircuitBreakerPolicy _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
	protected readonly ConcurrentDictionary<string, bool> _declaredExchanges = new();
	protected readonly ConcurrentDictionary<string, bool> _declaredQueues = new();

	#endregion
	#region Constructor
	#endregion

	#region IMessagePublisher Implementation
	public async Task<Result> PublishAsync(OutboxMessage message)
	{
		if (message == null)
			return Result.Failure(new Error("MessageNull", "Message cannot be null."));

		try
		{

			return await _circuitBreaker.ExecuteAsync(async () =>
			{
				return await _retryPolicy.ExecuteAsync(async () =>
				{
					var result = await EnsureExchangeAndQueueAsync();

					if (result.IsFailure)
						return result;

					using var connection = await CreateConnectionAsync();
					using var channel = await connection.CreateChannelAsync();

					//var serializedMessage = _serializer.Serialize(message);

					var compressedMessage = _compressor.Compress(Encoding.UTF8.GetBytes(message?.Payload));
					var encryptedMessage = _encryptor.Encrypt(compressedMessage);

					var properties = new BasicProperties
					{
						ContentType = "application/octet-stream",
						DeliveryMode = DeliveryModes.Persistent,
						Headers = new Dictionary<string, object?>
						{
							{ "CorrelationId", Guid.NewGuid().ToString() },
							{ "Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
						},
						Type = message.EventType // Set the fully qualified type name
					};

					await channel.BasicPublishAsync(
						_settings.Exchange,
						_settings.RoutingKey,
						mandatory: true,
						properties,
						encryptedMessage);

					return Result.Success();
				});
			});
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("MessagePublishError", "An error occurred while publishing message, with message : "+ ex.Message));
		}
	}
	#endregion

	#region Utilities
	protected async Task<Result> EnsureExchangeAndQueueAsync()
	{
		try
		{
			using var connection = await CreateConnectionAsync();
			using var channel = await connection.CreateChannelAsync();

			if (!_declaredExchanges.ContainsKey(_settings.Exchange))
			{
				await channel.ExchangeDeclareAsync(_settings.Exchange, ExchangeType.Direct, durable: true);
				_declaredExchanges.TryAdd(_settings.Exchange, true);
			}

			if (!_declaredQueues.ContainsKey(_settings.Queue))
			{
				await channel.QueueDeclareAsync(_settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
				await channel.QueueBindAsync(_settings.Queue, _settings.Exchange, _settings.RoutingKey, arguments: null);
				_declaredQueues.TryAdd(_settings.Queue, true);
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("ExchangeQueueDeclarationError", "An error occurred while declaring exchange and queue, with messsage : " + ex.Message));
		}
	}
	protected async Task<IConnection> CreateConnectionAsync()
	{
		var factory = new ConnectionFactory
		{
			HostName = _settings.HostName,
			UserName = _settings.UserName,
			Password = _settings.Password,
			VirtualHost = _settings.VirtualHost,
			Port = _settings.Port,
			ClientProvidedName = _settings.ClientProvidedName,
		};

		return await factory.CreateConnectionAsync();
	}
	#endregion

	#region IDisposable Implementation
	public void Dispose()
	{
	}
	#endregion
}
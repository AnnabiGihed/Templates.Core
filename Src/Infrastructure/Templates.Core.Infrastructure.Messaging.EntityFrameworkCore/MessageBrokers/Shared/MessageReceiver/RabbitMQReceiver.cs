using MediatR;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.RabbitMQ.Models;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageReceiver;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageCompressor;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageReceiver;

public class RabbitMQReceiver(IOptions<RabbitMQSettings> options, ILogger<RabbitMQReceiver> logger,	IMessageCompressor messageCompressor,	IMessageEncryptor messageEncryptor,	IMediator mediator, IServiceProvider serviceProvider) : IMessageReceiver
{
	#region Properties
	protected IChannel? _channel;
	protected IConnection? _connection;
	protected readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	protected readonly ILogger<RabbitMQReceiver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	protected readonly RabbitMQSettings _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
	protected readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	protected readonly IMessageEncryptor _messageEncryptor = messageEncryptor ?? throw new ArgumentNullException(nameof(messageEncryptor));
	protected readonly IMessageCompressor _messageCompressor = messageCompressor ?? throw new ArgumentNullException(nameof(messageCompressor));
	#endregion

	#region IMessageReceiver Implementation
	public async Task InitializeAsync()
	{
		var factory = new ConnectionFactory
		{
			Port = _settings.Port,
			HostName = _settings.HostName,
			UserName = _settings.UserName,
			Password = _settings.Password,
			VirtualHost = _settings.VirtualHost,
			ClientProvidedName = $"{_settings.ClientProvidedName}-receiver"
		};

		_connection = await factory.CreateConnectionAsync();
		_channel = await _connection.CreateChannelAsync();

		await EnsureQueueExistsAsync();
	}
	public async Task StartListeningAsync()
	{
		if (_connection == null || _channel == null)
		{
			throw new InvalidOperationException("RabbitMQReceiver is not initialized. Call InitializeAsync() first.");
		}

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (model, ea) =>
		{
			try
			{
				using var scope = _serviceProvider.CreateScope(); // Create a scope for resolving scoped services
				var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

				var encryptedMessage = ea.Body.ToArray();
				var compressedMessage = _messageEncryptor.Decrypt(encryptedMessage);
				var messageBytes = _messageCompressor.Decompress(compressedMessage);

				var messagePayload = Encoding.UTF8.GetString(messageBytes);
				_logger.LogInformation($"Message received: {messagePayload}");

				// Deserialize the payload into the appropriate domain event
				var domainEventType = Type.GetType(ea.BasicProperties.Type);
				_logger.LogWarning($"Retrived Domain Event Type: {ea.BasicProperties.Type}");

				if (domainEventType == null)
				{
					_logger.LogWarning($"Unknown event type: {ea.BasicProperties.Type}");
					await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
					return;
				}

				var domainEvent = JsonConvert.DeserializeObject(messagePayload, domainEventType);
				if (domainEvent is not INotification notificationEvent)
				{
					_logger.LogWarning($"Deserialized object is not an INotification: {ea.BasicProperties.Type}");
					await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
					return;
				}

				// Use Mediator to handle the domain event
				await scopedMediator.Publish(notificationEvent);

				// Acknowledge the message
				await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing message. Message will be requeued.");
				await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
			}
		};

		await _channel.BasicConsumeAsync(
			queue: _settings.Queue,
			autoAck: false,
			consumer: consumer);

		_logger.LogInformation($"Started listening on queue: {_settings.Queue}");
	}
	#endregion

	#region Utilities
	protected async Task EnsureQueueExistsAsync()
	{
		if (_channel == null) throw new InvalidOperationException("Channel is not initialized.");

		await _channel.QueueDeclareAsync(
			queue: _settings.Queue,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_logger.LogInformation($"Ensured queue '{_settings.Queue}' exists.");
	}
	#endregion

	#region IDisposable Implementation
	public void Dispose()
	{
		_channel?.CloseAsync();
		_connection?.CloseAsync();

		_channel?.Dispose();
		_connection?.Dispose();
	}
	#endregion
}

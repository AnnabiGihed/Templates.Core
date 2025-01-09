using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessagePublisher;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.Outbox.Services;

public class OutboxPublisherService : BackgroundService
{

	protected readonly IOutboxRepository _outboxRepository;
	protected readonly IMessagePublisher _messagePublisher;
	protected readonly ILogger<OutboxPublisherService> _logger;

	public OutboxPublisherService(IOutboxRepository outboxRepository, IMessagePublisher messagePublisher,
								   ILogger<OutboxPublisherService> logger)
	{
		_outboxRepository = outboxRepository;
		_messagePublisher = messagePublisher;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var messages = await _outboxRepository.GetUnprocessedMessagesAsync(stoppingToken);

			foreach (var message in messages)
			{
				try
				{
					await _messagePublisher.PublishAsync(message.Payload);
					await _outboxRepository.MarkAsProcessedAsync(message.Id, stoppingToken);
					_logger.LogInformation($"Message {message.Id} published successfully.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error publishing message {message.Id}");
				}
			}
		}
	}
}

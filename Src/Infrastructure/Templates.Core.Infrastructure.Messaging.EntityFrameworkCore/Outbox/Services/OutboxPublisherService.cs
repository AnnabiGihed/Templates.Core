using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessagePublisher;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.Outbox.Services;

public class OutboxPublisherService<TContext> : BackgroundService where TContext : DbContext
{
	protected readonly IServiceProvider _serviceProvider;
	protected readonly ILogger<OutboxPublisherService<TContext>> _logger;

	public OutboxPublisherService(IServiceProvider serviceProvider, ILogger<OutboxPublisherService<TContext>> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository<TContext>>();
				var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

				var messages = await outboxRepository.GetUnprocessedMessagesAsync(stoppingToken);

				foreach (var message in messages)
				{
					try
					{
						await messagePublisher.PublishAsync(message.Payload);
						await outboxRepository.MarkAsProcessedAsync(message.Id, stoppingToken);
						_logger.LogInformation($"Message {message.Id} published successfully.");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, $"Error publishing message {message.Id}");
					}
					// Add a delay to avoid tight loops
					await Task.Delay(1000, stoppingToken);
				}
			}
		}
	}
}

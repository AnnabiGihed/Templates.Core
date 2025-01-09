using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.Outbox.Services;

public class OutboxPublisherService<TContext>(IServiceProvider serviceProvider, ILogger<OutboxPublisherService<TContext>> logger) : BackgroundService where TContext : DbContext
{
	#region Properties
	protected readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	protected readonly ILogger<OutboxPublisherService<TContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	#endregion

	#region BackgroundService Overrides
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
						await messagePublisher.PublishAsync(message);
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
	#endregion
}

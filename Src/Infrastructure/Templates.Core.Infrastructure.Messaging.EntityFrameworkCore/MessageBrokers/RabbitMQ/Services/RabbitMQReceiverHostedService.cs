using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageReceiver;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.RabbitMQ.Services;

public class RabbitMQReceiverHostedService : BackgroundService
{
	#region Properties
	protected bool _isListening = false;
	protected readonly object _lock = new();
	protected readonly IMessageReceiver _messageReceiver;
	protected readonly IHostApplicationLifetime _applicationLifetime;
	protected readonly ILogger<RabbitMQReceiverHostedService> _logger;
	#endregion

	#region Constructor
	public RabbitMQReceiverHostedService(IMessageReceiver messageReceiver, ILogger<RabbitMQReceiverHostedService> logger, IHostApplicationLifetime applicationLifetime)
	{
		_messageReceiver = messageReceiver;
		_logger = logger;
		_applicationLifetime = applicationLifetime;
	}
	#endregion

	#region BackgroundService Methods
	public override async Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Stopping RabbitMQ receiver...");
		lock (_lock)
		{
			_isListening = false;
		}
		_messageReceiver.Dispose();
		await base.StopAsync(stoppingToken);
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_applicationLifetime.ApplicationStarted.Register(() =>
		{
			_logger.LogInformation("Application started. Preparing to initialize RabbitMQ receiver...");

			lock (_lock)
			{
				if (_isListening)
				{
					_logger.LogInformation("RabbitMQ receiver is already started. Skipping initialization.");
					return;
				}

				_isListening = true; // Mark as started
			}

			Task.Run(async () =>
			{
				try
				{
					_logger.LogInformation("Initializing RabbitMQ receiver...");
					await _messageReceiver.InitializeAsync();
					await _messageReceiver.StartListeningAsync();
					_logger.LogInformation("RabbitMQ receiver is now listening.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to start RabbitMQ receiver.");
					lock (_lock)
					{
						_isListening = false; // Reset the flag if initialization fails
					}
				}
			});
		});
	}
	#endregion
}
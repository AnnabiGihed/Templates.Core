using Polly;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.Repositories;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.RabbitMQ.Models;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageReceiver;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageSerializer;
using Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageCompressor;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.RabbitMQ.Services;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageReceiver;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessagePublisher;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageEncryptor;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageSerializer;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageCompressor;

namespace Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.RabbitMQ;
public static class RabbitMQPublisherExtensions
{
	public static IServiceCollection AddRabbitMQPublisher(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RabbitMQSettings>(options => configuration.GetSection("RabbitMQ").Bind(options));

		services.AddLogging();

		#region Polly
		services.AddSingleton(provider => 
			Policy.Handle<Exception>()
			.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
		);
		services.AddSingleton(provider =>
			Policy.Handle<Exception>()
				.CircuitBreakerAsync(2, TimeSpan.FromMinutes(1))
		);
		#endregion

		#region Message Broker
		services.AddSingleton<IMessageEncryptor>(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
			return new AesMessageEncryptor(settings.EncryptionKey);
		});
		services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
		services.AddSingleton<IMessageCompressor, GZipMessageCompressor>();
		services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
		services.AddSingleton<IMessageReceiver, RabbitMQReceiver>();
		services.AddHostedService<RabbitMQReceiverHostedService>();
		#endregion

		services.AddScoped(typeof(IOutboxRepository<>), typeof(OutboxRepository<>));
		return services;
	}
}
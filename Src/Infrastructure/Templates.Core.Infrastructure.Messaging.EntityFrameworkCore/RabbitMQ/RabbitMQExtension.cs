using Polly;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.Outbox.Services;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessageReceiver;
using Temlates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessageCompressor;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessageEncryptor;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessagePublisher;
using Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ.MessageSerializer;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.RabbitMQ;
public static class RabbitMQPublisherExtensions
{
	public static IServiceCollection AddRabbitMQPublisher(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RabbitMQSettings>(options => configuration.GetSection("RabbitMQ").Bind(options));

		services.AddLogging();
		services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
		services.AddSingleton<IMessageCompressor, GZipMessageCompressor>();
		services.AddSingleton<IMessageEncryptor>(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
			return new AesMessageEncryptor(settings.EncryptionKey);
		});

		services.AddSingleton(provider =>
			Policy.Handle<Exception>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
		);

		services.AddSingleton(provider =>
			Policy.Handle<Exception>()
				.CircuitBreakerAsync(2, TimeSpan.FromMinutes(1))
		);

		services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
		services.AddSingleton<IMessageReceiver, RabbitMQReceiver>();

		services.AddHostedService<OutboxPublisherService>();

		return services;
	}

}
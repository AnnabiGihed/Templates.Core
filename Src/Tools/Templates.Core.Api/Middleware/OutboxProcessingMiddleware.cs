using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Abstraction.Outbox.Processor;

namespace Templates.Core.Containers.API.Middleware;
public class OutboxProcessingMiddleware<TContext>(RequestDelegate next, ILogger<OutboxProcessingMiddleware<TContext>> logger) where TContext : DbContext
{
	#region Properties
	protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	protected readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
	#endregion

	#region Middleware Implementation
	public async Task InvokeAsync(HttpContext context)
	{
		// Proceed with the next middleware
		await _next(context);

		// Process outbox messages after the request completes
		try
		{
			var dbContext = context.RequestServices.GetRequiredService<TContext>();
			var outboxProcessor = context.RequestServices.GetRequiredService<IOutboxProcessor<TContext>>();

			if (outboxProcessor == null)
			{
				_logger.LogWarning("OutboxProcessor is not registered in the service container.");
				return;
			}

			_logger.LogInformation("Starting outbox processing.");
			var result = await outboxProcessor.ProcessOutboxMessagesAsync(context.RequestAborted);

			if (result.IsFailure)
			{
				_logger.LogError("Failed to process outbox messages: {Error}", result.Error);
			}
			else
			{
				_logger.LogInformation("Outbox processing completed successfully.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing outbox messages.");
		}
	}
	#endregion
}
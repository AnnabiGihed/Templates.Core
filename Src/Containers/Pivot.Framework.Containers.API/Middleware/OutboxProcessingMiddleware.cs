using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.Processor;

namespace Pivot.Framework.Containers.API.Middleware;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Processes outbox messages AFTER request completion and transaction commit.
///              Must be registered BEFORE TransactionMiddleware.
/// </summary>
public sealed class OutboxProcessingMiddleware<TContext> where TContext : DbContext
{
	private readonly RequestDelegate _next;
	private readonly ILogger<OutboxProcessingMiddleware<TContext>> _logger;

	public OutboxProcessingMiddleware(RequestDelegate next, ILogger<OutboxProcessingMiddleware<TContext>> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		await _next(context);

		if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
			return;

		try
		{
			var processor = context.RequestServices.GetService<IOutboxProcessor<TContext>>();

			if (processor is null)
				return;

			await processor.ProcessOutboxMessagesAsync(context.RequestAborted);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing outbox messages.");
		}
	}
}

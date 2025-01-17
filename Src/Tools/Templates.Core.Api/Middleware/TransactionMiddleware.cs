using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Infrastructure.Abstraction.Transaction;

namespace Templates.Core.Containers.API.Middleware;

public class TransactionMiddleware<TContext> where TContext : DbContext
{
	private readonly RequestDelegate _next;
	private readonly ILogger<TransactionMiddleware<TContext>> _logger;

	public TransactionMiddleware(RequestDelegate next, ILogger<TransactionMiddleware<TContext>> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Resolve DbContext within the request scope
		var dbContext = context.RequestServices.GetRequiredService<TContext>();
		var transactionManager = context.RequestServices.GetRequiredService<ITransactionManager<TContext>>();

		// Skip transaction for GET requests
		if (context.Request.Method == HttpMethods.Get)
		{
			_logger.LogInformation("Skipping transaction for GET request.");
			await _next(context);
			return;
		}

		try
		{
			// Begin a transaction for write operations
			_logger.LogInformation("Starting transaction for {Method} request to {Path}.", context.Request.Method, context.Request.Path);

			await transactionManager.BeginTransactionAsync();

			await _next(context);

			// Check response status code
			if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
			{
				await transactionManager.CommitTransactionAsync();
				_logger.LogInformation("Transaction committed successfully for {Method} request to {Path}.", context.Request.Method, context.Request.Path);

			}
			else
			{
				await transactionManager.RollbackTransactionAsync();
				_logger.LogWarning("Transaction rolled back due to non-success status code: {StatusCode}", context.Response.StatusCode);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing the request. Rolling back the transaction.");
			
			await transactionManager.RollbackTransactionAsync();
		}
	}
}
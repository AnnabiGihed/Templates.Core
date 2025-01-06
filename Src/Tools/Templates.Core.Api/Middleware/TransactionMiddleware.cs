using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Templates.Core.Tools.API.Middleware;

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

		using var transaction = await dbContext.Database.BeginTransactionAsync();

		try
		{
			await _next(context);

			if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
			{
				await transaction.CommitAsync();
			}
			else
			{
				await transaction.RollbackAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing the request. Rolling back the transaction.");
			await transaction.RollbackAsync();
			throw;
		}
	}
}

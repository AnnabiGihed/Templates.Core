using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Templates.Core.Tools.API.Middleware;

public class TransactionMiddleware<TContext> where TContext : DbContext
{
	protected readonly TContext _dbContext;
	protected readonly RequestDelegate _next;
	protected readonly ILogger<TransactionMiddleware<TContext>> _logger;

	public TransactionMiddleware(RequestDelegate next, ILogger<TransactionMiddleware<TContext>> logger, TContext dbContext)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		using var transaction = await _dbContext.Database.BeginTransactionAsync();

		try
		{
			await _next(context);

			if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
				await transaction.CommitAsync();
			else
				await transaction.RollbackAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing the request. Rolling back the transaction.");

			await transaction.RollbackAsync();

			if (context.Response.StatusCode == StatusCodes.Status200OK)
			{
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsync("An error occurred while processing the request. Please try again later.");
			}
		}
	}
}
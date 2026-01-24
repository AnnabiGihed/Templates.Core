using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Templates.Core.Infrastructure.Abstraction.Transaction;

namespace Templates.Core.Containers.API.Middleware;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Wraps non-GET requests in a database transaction.
///              Transaction ownership is at middleware level (not UnitOfWork).
/// </summary>
public sealed class TransactionMiddleware<TContext> where TContext : DbContext
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
		var transactionManager = context.RequestServices.GetRequiredService<ITransactionManager<TContext>>();

		if (context.Request.Method == HttpMethods.Get)
		{
			await _next(context);
			return;
		}

		await transactionManager.BeginTransactionAsync();

		try
		{
			await _next(context);

			if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
			{
				await transactionManager.RollbackTransactionAsync();
				return;
			}

			await transactionManager.CommitTransactionAsync();
		}
		catch
		{
			await transactionManager.RollbackTransactionAsync();
			throw; // CRITICAL: do not swallow exceptions
		}
	}
}

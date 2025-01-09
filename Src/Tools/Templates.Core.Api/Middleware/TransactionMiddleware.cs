using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

		using var transaction = await dbContext.Database.BeginTransactionAsync();

		try
		{
			// Capture the original response body
			var originalResponseBodyStream = context.Response.Body;
			using var memoryStream = new MemoryStream();
			context.Response.Body = memoryStream;

			// Proceed with the next middleware
			await _next(context);

			// Check response status code
			if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
			{
				await transaction.CommitAsync();

				// Read and log the response body
				memoryStream.Seek(0, SeekOrigin.Begin);
				var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
				_logger.LogInformation("Transaction succeeded with response: {ResponseBody}", responseBody);

				// Reset the memory stream and copy it back to the original response
				memoryStream.Seek(0, SeekOrigin.Begin);
				await memoryStream.CopyToAsync(originalResponseBodyStream);
			}
			else
			{
				await transaction.RollbackAsync();
				_logger.LogWarning("Transaction rolled back due to non-success status code: {StatusCode}", context.Response.StatusCode);

				// Optional: You can modify the response body or status code here if needed.
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing the request. Rolling back the transaction.");

			await transaction.RollbackAsync();

			// Return an error response
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
		}
		finally
		{
			// Restore the original response body
			context.Response.Body = context.Response.Body;
		}
	}
}
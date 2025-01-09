using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

public class OutboxRepository<TContext>(TContext dbContext) : IOutboxRepository<TContext> where TContext : DbContext
{
	#region Properties
	protected readonly TContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	#endregion

	#region IOutboxRepository Implementation
	public async Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
	{
		try
		{
			await _dbContext.Set<OutboxMessage>().AddAsync(message, cancellationToken);
			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("AddOutboxMessageError", "An error occurred while adding the message to the outbox. With Message: " + ex.Message));
		}
	}
	public async Task<Result> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
	{
		try
		{
			var message = await _dbContext.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
			if (message != null)
			{
				message.Processed = true;
				message.ProcessedAtUtc = DateTime.UtcNow;

				_dbContext.Update(message);

				await _dbContext.SaveChangesAsync(cancellationToken);
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("MarkAsProcessedError", "An error occurred while marking the message as processed. With Message: " + ex.Message));
		}
	}
	public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.Set<OutboxMessage>().Where(m => !m.Processed).OrderBy(m => m.CreatedAtUtc).ToListAsync(cancellationToken);
	}
	#endregion
}
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

public sealed class OutboxRepository<TContext>(TContext dbContext) : IOutboxRepository<TContext>
	where TContext : DbContext
{
	private readonly TContext _dbContext = dbContext;

	public async Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
	{
		try
		{
			await _dbContext.Set<OutboxMessage>().AddAsync(message, cancellationToken);
			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("AddOutboxMessageError", ex.Message));
		}
	}

	public async Task<Result> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
	{
		try
		{
			var message = await _dbContext.Set<OutboxMessage>()
				.FindAsync(new object[] { messageId }, cancellationToken);

			if (message is null)
				return Result.Success();

			message.Processed = true;
			message.ProcessedAtUtc = DateTime.UtcNow;

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("MarkAsProcessedError", ex.Message));
		}
	}

	public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
	{
		return _dbContext.Set<OutboxMessage>()
			.Where(m => !m.Processed)
			.OrderBy(m => m.CreatedAtUtc)
			.ToListAsync(cancellationToken)
			.ContinueWith(t => (IReadOnlyList<OutboxMessage>)t.Result, cancellationToken);
	}
}

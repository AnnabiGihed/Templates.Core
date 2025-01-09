using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.Outbox.Repositories;

public class OutboxRepository<TContext> : IOutboxRepository where TContext : DbContext
{
	private readonly TContext _dbContext;

	public OutboxRepository(TContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
	{
		await _dbContext.Set<OutboxMessage>().AddAsync(message, cancellationToken);
	}

	public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.Set<OutboxMessage>()
			.Where(m => !m.Processed)
			.OrderBy(m => m.CreatedAtUtc)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
	{
		var message = await _dbContext.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
		if (message != null)
		{
			message.Processed = true;
			message.ProcessedAtUtc = DateTime.UtcNow;
			_dbContext.Update(message);
		}
	}
}
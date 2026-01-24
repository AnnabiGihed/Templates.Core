using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;

namespace Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Repository for managing outbox messages.
///              SaveChanges is intentionally NOT called inside repository methods.
/// </summary>
public interface IOutboxRepository<TContext> where TContext : DbContext
{
	Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

	Task<Result> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);
}

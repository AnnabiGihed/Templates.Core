using Microsoft.EntityFrameworkCore;

namespace Templates.Core.Infrastructure.Abstraction.Transaction;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Abstraction for managing EF Core transactions.
///              Designed to be used by middleware to own the transaction boundary.
/// </summary>
public interface ITransactionManager<TContext> where TContext : DbContext
{
	Task BeginTransactionAsync(CancellationToken cancellationToken = default);
	Task CommitTransactionAsync(CancellationToken cancellationToken = default);
	Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

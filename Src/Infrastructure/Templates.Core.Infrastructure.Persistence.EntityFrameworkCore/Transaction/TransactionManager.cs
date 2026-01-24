using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Templates.Core.Infrastructure.Abstraction.Transaction;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : EF Core transaction manager.
///              - Idempotent (won't start a new transaction if one already exists)
///              - Commits/Rolls back only when a transaction exists
///              - Disposes the transaction after completion to avoid leaks
/// </summary>
public sealed class TransactionManager<TContext> : ITransactionManager<TContext>
	where TContext : DbContext
{
	private readonly TContext _dbContext;

	public TransactionManager(TContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		// Avoid nested/duplicate transactions
		if (_dbContext.Database.CurrentTransaction is not null)
			return;

		await _dbContext.Database.BeginTransactionAsync(cancellationToken);
	}

	public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
	{
		var tx = _dbContext.Database.CurrentTransaction;
		if (tx is null)
			return;

		try
		{
			await _dbContext.Database.CommitTransactionAsync(cancellationToken);
		}
		finally
		{
			await DisposeCurrentTransactionAsync(tx);
		}
	}

	public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
	{
		var tx = _dbContext.Database.CurrentTransaction;
		if (tx is null)
			return;

		try
		{
			await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
		}
		finally
		{
			await DisposeCurrentTransactionAsync(tx);
		}
	}

	private static async Task DisposeCurrentTransactionAsync(IDbContextTransaction tx)
	{
		// Ensures resources are released even if commit/rollback throws
		await tx.DisposeAsync();
	}
}

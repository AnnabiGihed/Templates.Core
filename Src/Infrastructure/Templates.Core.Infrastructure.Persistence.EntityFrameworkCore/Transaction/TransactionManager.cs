using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Abstraction.Transaction;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;

public class TransactionManager<TContext> : ITransactionManager<TContext> where TContext : DbContext
{
	private readonly TContext _dbContext;

	public TransactionManager(TContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task BeginTransactionAsync()
	{
		await _dbContext.Database.BeginTransactionAsync();
	}

	public async Task CommitTransactionAsync()
	{
		if (_dbContext.Database.CurrentTransaction != null)
		{
			await _dbContext.Database.CommitTransactionAsync();
		}
	}

	public async Task RollbackTransactionAsync()
	{
		if (_dbContext.Database.CurrentTransaction != null)
		{
			await _dbContext.Database.RollbackTransactionAsync();
		}
	}
}
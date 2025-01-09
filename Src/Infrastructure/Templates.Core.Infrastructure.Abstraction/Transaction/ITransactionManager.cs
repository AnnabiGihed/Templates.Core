using Microsoft.EntityFrameworkCore;

namespace Templates.Core.Infrastructure.Abstraction.Transaction;

public interface ITransactionManager<TContext> where TContext : DbContext
{
	Task BeginTransactionAsync();
	Task CommitTransactionAsync();
	Task RollbackTransactionAsync();
}
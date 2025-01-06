using Microsoft.EntityFrameworkCore;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;

public interface ITransactionManager<TContext> where TContext : DbContext
{
	Task BeginTransactionAsync();
	Task CommitTransactionAsync();
	Task RollbackTransactionAsync();
}
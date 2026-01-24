using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : EF Core base command repository for aggregate roots.
///              Soft deletes by default when supported.
/// </summary>
public class BaseAsyncCommandRepository<TEntity, TId> : IAsyncCommandRepository<TEntity, TId>
	where TEntity : AggregateRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	protected readonly DbContext DbContext;

	public BaseAsyncCommandRepository(DbContext dbContext)
	{
		DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
	}

	public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var entry = DbContext.Entry(entity);
		if (entry.State == EntityState.Detached)
			DbContext.Set<TEntity>().Attach(entity);

		entry.State = EntityState.Modified;
		return Task.CompletedTask;
	}

	public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);

		// Soft delete is default.
		if (entity is ISoftDeletableEntity soft)
		{
			soft.MarkDeleted(DateTime.UtcNow, "System"); // actor can be injected if you want
			DbContext.Entry(entity).State = EntityState.Modified;
			return Task.CompletedTask;
		}

		DbContext.Set<TEntity>().Remove(entity);
		return Task.CompletedTask;
	}
}

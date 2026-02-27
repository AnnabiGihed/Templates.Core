using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : EF Core base implementation of <see cref="IAsyncCommandRepository{TEntity,TId}"/>.
///              Provides Add, Update, Delete, FindById, and Exists operations for aggregate roots so
///              that concrete repositories inherit full basic behaviour without re-implementing it.
///              Delete is soft by default when the entity implements <see cref="ISoftDeletableEntity"/>;
///              otherwise the entity is hard-deleted.
///              Designed for inheritance — concrete repositories may override any virtual method to
///              add domain-specific behaviour.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type managed by this repository.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier type of the aggregate root.</typeparam>
public class BaseAsyncCommandRepository<TEntity, TId> : IAsyncCommandRepository<TEntity, TId>
	where TEntity : AggregateRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	#region Fields
	/// <summary>
	/// The EF Core <see cref="DbContext"/> used by this repository.
	/// Exposed as <c>protected</c> so that derived repositories can access it for custom queries.
	/// </summary>
	protected readonly DbContext DbContext;
	#endregion

	#region Constructors
	/// <summary>
	/// Initialises a new <see cref="BaseAsyncCommandRepository{TEntity,TId}"/> with the provided
	/// <see cref="DbContext"/>.
	/// </summary>
	/// <param name="dbContext">The EF Core database context. Must not be null.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
	public BaseAsyncCommandRepository(DbContext dbContext)
	{
		DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}
	#endregion

	#region Public Methods
	/// <summary>
	/// Adds a new <typeparamref name="TEntity"/> to the EF Core change tracker.
	/// The entity is inserted into the database when the ambient unit of work commits.
	/// </summary>
	/// <param name="entity">The aggregate root to add. Must not be null.</param>
	/// <param name="ct">Token to observe for cooperative cancellation.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
	public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		await DbContext.Set<TEntity>().AddAsync(entity, ct);
	}

	/// <summary>
	/// Marks an existing <typeparamref name="TEntity"/> as <see cref="EntityState.Modified"/> in the
	/// EF Core change tracker. If the entity is detached, it is first re-attached.
	/// Changes are written to the database when the ambient unit of work commits.
	/// </summary>
	/// <param name="entity">The aggregate root to update. Must not be null.</param>
	/// <param name="ct">Token to observe for cooperative cancellation (not used directly; included for interface parity).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
	public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var entry = DbContext.Entry(entity);

		if (entry.State == EntityState.Detached)
		{
			DbContext.Set<TEntity>().Attach(entity);
		}

		entry.State = EntityState.Modified;

		return Task.CompletedTask;
	}

	/// <summary>
	/// Deletes the specified <typeparamref name="TEntity"/>.
	/// When the entity implements <see cref="ISoftDeletableEntity"/>, the entity is soft-deleted
	/// (flagged only, remains in the database). Otherwise, it is scheduled for physical removal
	/// on the next commit.
	/// </summary>
	/// <param name="entity">The aggregate root to delete. Must not be null.</param>
	/// <param name="ct">Token to observe for cooperative cancellation (not used directly; included for interface parity).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
	public virtual Task DeleteAsync(TEntity entity, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(entity);

		if (entity is ISoftDeletableEntity soft)
		{
			soft.MarkDeleted(DateTime.UtcNow, "System");
			DbContext.Entry(entity).State = EntityState.Modified;
			return Task.CompletedTask;
		}

		DbContext.Set<TEntity>().Remove(entity);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Returns the <typeparamref name="TEntity"/> whose primary key equals <paramref name="id"/>,
	/// or <c>null</c> when no match is found.
	/// The entity is change-tracked so that mutations are picked up by the unit of work on commit.
	/// </summary>
	/// <param name="id">The strongly-typed identifier to look up. Must not be null.</param>
	/// <param name="ct">Token to observe for cooperative cancellation.</param>
	/// <returns>The matching entity, or <c>null</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
	public virtual Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(id);

		return DbContext.Set<TEntity>()
			.FirstOrDefaultAsync(x => x.Id!.Equals(id), ct);
	}

	/// <summary>
	/// Returns <c>true</c> when at least one <typeparamref name="TEntity"/> in the store satisfies
	/// <paramref name="predicate"/>; <c>false</c> otherwise.
	/// Runs as a lightweight server-side <c>EXISTS</c> check — no entity is materialised.
	/// </summary>
	/// <param name="predicate">The LINQ predicate to evaluate server-side. Must not be null.</param>
	/// <param name="ct">Token to observe for cooperative cancellation.</param>
	/// <returns><c>true</c> if a matching entity exists; otherwise <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
	public virtual Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		return DbContext.Set<TEntity>()
			.AnyAsync(predicate, ct);
	}
	#endregion
}
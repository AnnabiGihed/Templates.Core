using System.Linq.Expressions;
using Pivot.Framework.Domain.Primitives;

namespace Pivot.Framework.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines write operations for aggregate roots, plus the
///              read-side lookups every command repository needs so that
///              concrete repositories do not have to re-declare them.
///              Commands modify state and are responsible for persistence
///              of changes.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
public interface IAsyncCommandRepository<TEntity, TId>
	where TEntity : AggregateRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	/// <summary>
	/// Returns the <typeparamref name="TEntity"/> with the given <paramref name="id"/>,
	/// or <c>null</c> when no match is found.
	/// The entity is change-tracked so the unit of work can persist mutations.
	/// </summary>
	Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default);

	/// <summary>
	/// Adds a new <typeparamref name="TEntity"/> to the persistence store.
	/// The entity is tracked by the EF Core change tracker until the unit of work commits.
	/// </summary>
	/// <param name="entity">The aggregate root to add. Must not be null.</param>
	/// <param name="cancellationToken">Token to observe for cooperative cancellation.</param>
	Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Marks an existing <typeparamref name="TEntity"/> as modified in the EF Core change tracker.
	/// Changes are persisted when the unit of work commits.
	/// </summary>
	/// <param name="entity">The aggregate root to update. Must not be null.</param>
	/// <param name="cancellationToken">Token to observe for cooperative cancellation.</param>
	Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the specified <typeparamref name="TEntity"/>.
	/// When the entity implements <see cref="ISoftDeletableEntity"/>, deletion is soft (flagged only).
	/// Otherwise, the entity is removed from the database on commit.
	/// </summary>
	/// <param name="entity">The aggregate root to delete. Must not be null.</param>
	/// <param name="cancellationToken">Token to observe for cooperative cancellation.</param>
	Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns <c>true</c> when at least one <typeparamref name="TEntity"/> satisfies
	/// <paramref name="predicate"/>; <c>false</c> otherwise.
	/// </summary>
	Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
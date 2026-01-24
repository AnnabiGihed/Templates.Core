using Templates.Core.Domain.Primitives;

namespace Templates.Core.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines write operations for aggregate roots.
///              Commands modify state and are responsible for persistence of changes.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier type.</typeparam>

public interface IAsyncCommandRepository<TEntity, TId>
	where TEntity : AggregateRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
	Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
	Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default); // soft delete by default
}

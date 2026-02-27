using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a unit of work scoped to a specific DbContext type via the <typeparamref name="TId"/> marker.
///              Coordinates a single atomic database operation: auditing entity changes, flushing pending
///              domain events to the outbox, and committing via EF Core <c>SaveChangesAsync</c>.
///              The generic parameter <typeparamref name="TId"/> acts as a DI scoping key so that multiple
///              DbContexts can coexist in the same process without DI ambiguity.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier used as the DI scope discriminator.</typeparam>
public interface IUnitOfWork<TId>
	where TId : IStronglyTypedId<TId>
{
	/// <summary>
	/// Atomically persists all pending entity changes.
	/// Internally: stamps audit fields on modified entities, serialises raised domain events to the outbox,
	/// then calls EF Core <c>SaveChangesAsync</c> within the ambient transaction.
	/// </summary>
	/// <param name="cancellationToken">Token to observe for cooperative cancellation.</param>
	/// <returns>
	/// A <see cref="Result"/> that is successful when the commit succeeds, or a failure carrying
	/// the underlying database error (concurrency conflict, constraint violation, etc.).
	/// </returns>
	Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
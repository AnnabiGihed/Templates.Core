using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a unit of work scoped to a specific aggregate identifier type.
///              The generic parameter TId is used as a DI scoping key.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier used as the DI scope key.</typeparam>
public interface IUnitOfWork<TId>
	where TId : IStronglyTypedId<TId>
{
	/// <summary>
	/// Persists all pending changes as a single atomic operation.
	/// </summary>
	Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}

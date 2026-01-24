using System.Linq.Expressions;
using Templates.Core.Domain.Primitives;

namespace Templates.Core.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Read-only repository for projections (query models).
///              Uses expression-based predicates to keep evaluation server-side.
/// </summary>
public interface IAsyncQueryRepository<TProjection, TId>
	where TProjection : ProjectionRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	Task<TProjection?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

	Task<TProjection?> FirstOrDefaultAsync(
		Expression<Func<TProjection, bool>> predicate,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<TProjection>> ListAsync(
		Expression<Func<TProjection, bool>>? predicate = null,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<TProjection>> GetPagedAsync(
		int page,
		int size,
		Expression<Func<TProjection, bool>>? predicate = null,
		CancellationToken cancellationToken = default);

	Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}

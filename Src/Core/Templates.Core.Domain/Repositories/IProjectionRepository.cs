using Templates.Core.Domain.Primitives;

namespace Templates.Core.Domain.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Read-only repository for projections.
/// </summary>
public interface IProjectionRepository<TProjection, TId>
	where TProjection : ProjectionRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	Task<TProjection?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<TProjection>> ListAsync(CancellationToken cancellationToken = default);
}

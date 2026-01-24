using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : EF Core implementation of a read-only repository for projections.
///              Uses AsNoTracking by default to avoid tracking overhead and side effects.
/// </summary>
/// <typeparam name="TProjection">Projection root type (read model).</typeparam>
/// <typeparam name="TId">Strongly-typed identifier type.</typeparam>
public sealed class ProjectionRepository<TProjection, TId> : IProjectionRepository<TProjection, TId>
	where TProjection : ProjectionRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	private readonly DbContext _dbContext;

	/// <summary>
	/// Initializes a new instance of the projection repository.
	/// </summary>
	/// <param name="dbContext">The EF Core DbContext used to query projections.</param>
	public ProjectionRepository(DbContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	/// <summary>
	/// Retrieves a projection by its identifier.
	/// </summary>
	/// <param name="id">The projection identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The projection if found; otherwise null.</returns>
	public async Task<TProjection?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);

		// Use predicate-based query for consistency with AsNoTracking.
		return await _dbContext.Set<TProjection>()
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);
	}

	/// <summary>
	/// Lists all projections.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A read-only list of projections.</returns>
	public async Task<IReadOnlyList<TProjection>> ListAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.Set<TProjection>()
			.AsNoTracking()
			.ToListAsync(cancellationToken);
	}
}
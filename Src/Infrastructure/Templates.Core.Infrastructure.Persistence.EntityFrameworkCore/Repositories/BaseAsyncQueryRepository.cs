using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : EF Core base query repository for projection roots.
///              - Read-only (no Add/Update/Delete).
///              - Expression predicates to keep filtering server-side.
///              - Uses AsNoTracking by default.
/// </summary>
/// <typeparam name="TProjection">Projection root type (read model).</typeparam>
/// <typeparam name="TId">Strongly-typed identifier.</typeparam>
public class BaseAsyncQueryRepository<TProjection, TId> : IAsyncQueryRepository<TProjection, TId>
	where TProjection : ProjectionRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	protected readonly DbContext DbContext;

	public BaseAsyncQueryRepository(DbContext dbContext)
	{
		DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public virtual Task<TProjection?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);
		return DbContext.Set<TProjection>()
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);
	}

	public virtual Task<TProjection?> FirstOrDefaultAsync(
		Expression<Func<TProjection, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		return DbContext.Set<TProjection>()
			.AsNoTracking()
			.FirstOrDefaultAsync(predicate, cancellationToken);
	}

	public virtual async Task<IReadOnlyList<TProjection>> ListAsync(
		Expression<Func<TProjection, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		IQueryable<TProjection> query = DbContext.Set<TProjection>().AsNoTracking();

		if (predicate is not null)
			query = query.Where(predicate);

		return await query.ToListAsync(cancellationToken);
	}

	public virtual async Task<IReadOnlyList<TProjection>> GetPagedAsync(
		int page,
		int size,
		Expression<Func<TProjection, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));
		if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

		IQueryable<TProjection> query = DbContext.Set<TProjection>().AsNoTracking();

		if (predicate is not null)
			query = query.Where(predicate);

		return await query
			.Skip((page - 1) * size)
			.Take(size)
			.ToListAsync(cancellationToken);
	}

	public virtual Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);

		return DbContext.Set<TProjection>()
			.AsNoTracking()
			.AnyAsync(x => x.Id!.Equals(id), cancellationToken);
	}
}

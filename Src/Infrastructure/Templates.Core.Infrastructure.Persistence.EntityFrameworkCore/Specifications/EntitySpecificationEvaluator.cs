using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Specifications;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Applies an EntitySpecification to an IQueryable using EF Core.
///              Supports criteria, includes, ordering, paging, split queries,
///              tracking mode and default soft-delete filtering.
/// </summary>
public static class EntitySpecificationEvaluator
{
	public static IQueryable<TEntity> GetQuery<TEntity, TId>(
		IQueryable<TEntity> inputQueryable,
		EntitySpecification<TEntity, TId> specification)
		where TEntity : Entity<TId>
		where TId : IStronglyTypedId<TId>
	{
		ArgumentNullException.ThrowIfNull(inputQueryable);
		ArgumentNullException.ThrowIfNull(specification);

		IQueryable<TEntity> queryable = inputQueryable;

		// Tracking
		if (specification.AsNoTracking)
			queryable = queryable.AsNoTracking();

		// Soft delete filtering (default exclude)
		if (!specification.IncludeSoftDeleted && typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(TEntity)))
			queryable = queryable.Where(e => !((ISoftDeletableEntity)e).IsDeleted);

		// Criteria
		if (specification.Criteria is not null)
			queryable = queryable.Where(specification.Criteria);

		// Includes
		if (specification.IncludeExpressions.Count > 0)
		{
			queryable = specification.IncludeExpressions.Aggregate(
				queryable,
				(current, includeExpression) => current.Include(includeExpression));
		}

		// Ordering
		if (specification.OrderByExpression is not null)
			queryable = queryable.OrderBy(specification.OrderByExpression);
		else if (specification.OrderByDescendingExpression is not null)
			queryable = queryable.OrderByDescending(specification.OrderByDescendingExpression);

		// Paging (after ordering)
		if (specification.Skip.HasValue)
			queryable = queryable.Skip(specification.Skip.Value);

		if (specification.Take.HasValue)
			queryable = queryable.Take(specification.Take.Value);

		// Split query
		if (specification.IsSplitQuery)
			queryable = queryable.AsSplitQuery();

		return queryable;
	}
}

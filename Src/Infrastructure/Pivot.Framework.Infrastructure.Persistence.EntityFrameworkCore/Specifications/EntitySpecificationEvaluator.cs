using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Domain.Primitives;

namespace Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Specifications;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Applies an <see cref="EntitySpecification{TEntity,TId}"/> to an <see cref="IQueryable{T}"/>
///              using EF Core extension methods.
///              Handles soft-delete filtering, criteria predicates, eager-load includes,
///              ordering, offset/limit paging, split-query mode, and change-tracking configuration.
///              This is a pure static utility class — it holds no state and is thread-safe.
/// </summary>
public static class EntitySpecificationEvaluator
{
	#region Public Methods
	/// <summary>
	/// Transforms a base <see cref="IQueryable{TEntity}"/> by applying every clause declared in
	/// the provided <paramref name="specification"/> in the correct EF Core evaluation order:
	/// tracking → soft-delete filter → criteria → includes → ordering → paging → split-query.
	/// </summary>
	/// <typeparam name="TEntity">The entity type being queried. Must derive from <see cref="Entity{TId}"/>.</typeparam>
	/// <typeparam name="TId">The strongly-typed identifier type of <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="inputQueryable">
	/// The base <see cref="IQueryable{TEntity}"/> to transform — typically <c>DbContext.Set&lt;TEntity&gt;()</c>.
	/// Must not be null.
	/// </param>
	/// <param name="specification">
	/// The specification that encapsulates all query criteria, includes, ordering, and paging.
	/// Must not be null.
	/// </param>
	/// <returns>
	/// A new <see cref="IQueryable{TEntity}"/> with all specification clauses applied.
	/// The query is not yet materialised — it is evaluated only when enumerated.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="inputQueryable"/> or <paramref name="specification"/> is null.
	/// </exception>
	public static IQueryable<TEntity> GetQuery<TEntity, TId>(IQueryable<TEntity> inputQueryable, EntitySpecification<TEntity, TId> specification)
		where TEntity : Entity<TId>
		where TId : IStronglyTypedId<TId>
	{
		ArgumentNullException.ThrowIfNull(inputQueryable);
		ArgumentNullException.ThrowIfNull(specification);

		IQueryable<TEntity> queryable = inputQueryable;

		#region Tracking
		if (specification.AsNoTracking)
			queryable = queryable.AsNoTracking();
		#endregion

		#region Soft Delete Filter
		if (!specification.IncludeSoftDeleted && typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(TEntity)))
			queryable = queryable.Where(e => !((ISoftDeletableEntity)e).IsDeleted);
		#endregion

		#region Criteria
		if (specification.Criteria is not null)
			queryable = queryable.Where(specification.Criteria);
		#endregion

		#region Includes
		if (specification.IncludeExpressions.Count > 0)
			queryable = specification.IncludeExpressions.Aggregate(queryable, (current, includeExpression) => current.Include(includeExpression));
		#endregion

		#region Ordering
		if (specification.OrderByExpression is not null)
			queryable = queryable.OrderBy(specification.OrderByExpression);
		else if (specification.OrderByDescendingExpression is not null)
			queryable = queryable.OrderByDescending(specification.OrderByDescendingExpression);
		#endregion

		#region Paging
		if (specification.Skip.HasValue)
			queryable = queryable.Skip(specification.Skip.Value);

		if (specification.Take.HasValue)
			queryable = queryable.Take(specification.Take.Value);
		#endregion

		#region Split Query

		if (specification.IsSplitQuery)
			queryable = queryable.AsSplitQuery();
		#endregion

		return queryable;
	}
	#endregion
}
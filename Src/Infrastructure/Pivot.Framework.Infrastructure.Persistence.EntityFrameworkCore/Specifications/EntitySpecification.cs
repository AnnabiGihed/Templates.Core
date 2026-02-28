using System.Linq.Expressions;
using Pivot.Framework.Domain.Primitives;

namespace Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Specifications;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base specification for querying EF Core entities.
///              Encapsulates:
///              - filtering (Criteria)
///              - includes
///              - ordering
///              - paging
///              - tracking behavior
///              - split query directive
///              - optional inclusion of soft-deleted rows
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TId">Strongly-typed ID type.</typeparam>
public abstract class EntitySpecification<TEntity, TId>
	where TEntity : Entity<TId>
	where TId : IStronglyTypedId<TId>
{
	protected EntitySpecification(Expression<Func<TEntity, bool>>? criteria = null)
	{
		Criteria = criteria;
	}

	/// <summary>
	/// Gets the criteria (WHERE clause) of the specification.
	/// </summary>
	public Expression<Func<TEntity, bool>>? Criteria { get; }

	/// <summary>
	/// Gets include expressions to apply.
	/// </summary>
	public List<Expression<Func<TEntity, object>>> IncludeExpressions { get; } = new();

	/// <summary>
	/// Indicates whether EF Core should use split queries (AsSplitQuery).
	/// </summary>
	public bool IsSplitQuery { get; protected set; }

	/// <summary>
	/// Indicates whether the query should be executed as no-tracking.
	/// Default is true for query scenarios.
	/// </summary>
	public bool AsNoTracking { get; protected set; } = true;

	/// <summary>
	/// Indicates whether soft-deleted entities should be included.
	/// Default is false (exclude soft-deleted rows).
	/// </summary>
	public bool IncludeSoftDeleted { get; protected set; }

	/// <summary>
	/// Order by expression.
	/// </summary>
	public Expression<Func<TEntity, object>>? OrderByExpression { get; private set; }

	/// <summary>
	/// Order by descending expression.
	/// </summary>
	public Expression<Func<TEntity, object>>? OrderByDescendingExpression { get; private set; }

	/// <summary>
	/// Paging: number of rows to skip.
	/// </summary>
	public int? Skip { get; private set; }

	/// <summary>
	/// Paging: number of rows to take.
	/// </summary>
	public int? Take { get; private set; }

	/// <summary>
	/// Enables paging for the specification.
	/// </summary>
	/// <param name="skip">Number of rows to skip (0+).</param>
	/// <param name="take">Number of rows to take (1+).</param>
	protected void ApplyPaging(int skip, int take)
	{
		if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
		if (take <= 0) throw new ArgumentOutOfRangeException(nameof(take));

		Skip = skip;
		Take = take;
	}

	/// <summary>
	/// Adds an include expression (ignored if already present).
	/// </summary>
	protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
	{
		ArgumentNullException.ThrowIfNull(includeExpression);

		if (!IncludeExpressions.Contains(includeExpression))
			IncludeExpressions.Add(includeExpression);
	}

	/// <summary>
	/// Adds an ascending ordering expression.
	/// </summary>
	protected void AddOrderBy(Expression<Func<TEntity, object>> orderByExpression)
	{
		ArgumentNullException.ThrowIfNull(orderByExpression);

		OrderByExpression = orderByExpression;
		OrderByDescendingExpression = null;
	}

	/// <summary>
	/// Adds a descending ordering expression.
	/// </summary>
	protected void AddOrderByDescending(Expression<Func<TEntity, object>> orderByDescendingExpression)
	{
		ArgumentNullException.ThrowIfNull(orderByDescendingExpression);

		OrderByDescendingExpression = orderByDescendingExpression;
		OrderByExpression = null;
	}

	/// <summary>
	/// Enables tracked queries (turns off AsNoTracking).
	/// Useful for command-side reads.
	/// </summary>
	protected void EnableTracking() => AsNoTracking = false;

	/// <summary>
	/// Enables split query behavior.
	/// </summary>
	protected void EnableSplitQuery() => IsSplitQuery = true;

	/// <summary>
	/// Allows querying soft-deleted entities.
	/// </summary>
	protected void EnableIncludeSoftDeleted() => IncludeSoftDeleted = true;
}

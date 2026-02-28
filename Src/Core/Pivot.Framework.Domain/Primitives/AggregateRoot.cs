namespace Pivot.Framework.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for aggregate roots in a Domain-Driven Design model.
///              An aggregate root is the consistency boundary of a domain cluster.
///              It collects and exposes domain events raised during state transitions,
///              and inherits auditing and soft-delete behaviour from <see cref="Entity{TId}"/>.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier of the aggregate root.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
	where TId : IStronglyTypedId<TId>
{
	#region Fields
	/// <summary>
	/// Internal store of domain events raised during this aggregate's lifecycle.
	/// </summary>
	private readonly List<IDomainEvent> _domainEvents = new();
	#endregion

	#region Constructors
	/// <summary>
	/// Initialises a new <see cref="AggregateRoot{TId}"/> with the specified identity.
	/// </summary>
	/// <param name="id">The strongly-typed identifier for this aggregate root.</param>
	protected AggregateRoot(TId id) : base(id)
	{
	}

	/// <summary>
	/// Parameterless constructor reserved for EF Core materialisation.
	/// </summary>
	protected AggregateRoot()
	{
	}
	#endregion

	#region Domain Events
	/// <summary>
	/// Returns all domain events raised by this aggregate since the last time they were cleared.
	/// Events are dispatched by the infrastructure after the unit of work completes successfully.
	/// </summary>
	/// <returns>A read-only collection of pending domain events.</returns>
	public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
	{
		return _domainEvents.AsReadOnly();
	}

	/// <summary>
	/// Clears all stored domain events from this aggregate.
	/// Called by the infrastructure after events have been successfully dispatched.
	/// </summary>
	public void ClearDomainEvents()
	{
		_domainEvents.Clear();
	}

	/// <summary>
	/// Registers a new domain event to be dispatched after the current unit of work commits.
	/// </summary>
	/// <param name="domainEvent">The domain event to raise.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
	protected void RaiseDomainEvent(IDomainEvent domainEvent)
	{
		ArgumentNullException.ThrowIfNull(domainEvent);
		_domainEvents.Add(domainEvent);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Soft-deletes this aggregate root.
	/// Delegates to the protected <see cref="Entity{TId}.SoftDelete"/> method.
	/// </summary>
	/// <param name="deletedOnUtc">UTC timestamp of deletion.</param>
	/// <param name="deletedBy">Actor who performed the deletion.</param>
	protected void Delete(DateTime deletedOnUtc, string deletedBy)
	{
		SoftDelete(deletedOnUtc, deletedBy);
	}

	/// <summary>
	/// Restores a previously soft-deleted aggregate root.
	/// Delegates to the protected <see cref="Entity{TId}.Restore"/> method.
	/// </summary>
	/// <param name="restoredOnUtc">UTC timestamp of restoration.</param>
	/// <param name="restoredBy">Actor who performed the restoration.</param>
	protected void RestoreDeleted(DateTime restoredOnUtc, string restoredBy)
	{
		Restore(restoredOnUtc, restoredBy);
	}
	#endregion
}
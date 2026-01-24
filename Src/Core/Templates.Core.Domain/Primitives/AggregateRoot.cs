namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for aggregate roots in a Domain-Driven Design (DDD) model.
///              An aggregate root represents a consistency boundary and is responsible
///              for raising and collecting domain events during state changes.
///              It also inherits default entity behavior such as auditing and soft delete.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier of the aggregate root.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
	where TId : IStronglyTypedId<TId>
{
	private readonly List<IDomainEvent> _domainEvents = new();

	protected AggregateRoot(TId id) : base(id) { }

	protected AggregateRoot() { }

	public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

	public void ClearDomainEvents() => _domainEvents.Clear();

	protected void RaiseDomainEvent(IDomainEvent domainEvent)
	{
		ArgumentNullException.ThrowIfNull(domainEvent);
		_domainEvents.Add(domainEvent);
	}

	protected void Delete(DateTime deletedOnUtc, string deletedBy) => SoftDelete(deletedOnUtc, deletedBy);

	protected void RestoreDeleted(DateTime restoredOnUtc, string restoredBy) => Restore(restoredOnUtc, restoredBy);
}

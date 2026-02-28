namespace Pivot.Framework.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marker interface that defines the contract for aggregate roots in a DDD model.
///              Aggregate roots are consistency boundaries — they own child entities and enforce
///              all invariants within the aggregate cluster.
///              The infrastructure uses this interface to gather pending domain events from the
///              EF Core <c>ChangeTracker</c> after a successful <c>SaveChanges</c> call.
/// </summary>
public interface IAggregateRoot
{

	/// <summary>
	/// Clears all stored domain events from this aggregate.
	/// Called by the infrastructure after events have been successfully persisted to the outbox
	/// or dispatched to the message broker.
	/// </summary>
	void ClearDomainEvents();

	/// <summary>
	/// Returns all domain events raised by this aggregate since the last time they were cleared.
	/// Events represent business facts that occurred during the current unit of work.
	/// </summary>
	/// <returns>A read-only collection of pending <see cref="IDomainEvent"/> instances.</returns>
	IReadOnlyCollection<IDomainEvent> GetDomainEvents();
}
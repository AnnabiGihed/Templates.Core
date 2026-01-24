namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines the contract for aggregate roots in a Domain-Driven Design (DDD) model.
///              An aggregate root is responsible for maintaining consistency boundaries and
///              collecting domain events raised during state changes.
/// </summary>
public interface IAggregateRoot
{
	/// <summary>
	/// Gets the domain events raised by the aggregate since the last time they were cleared.
	/// These events are typically dispatched after the current unit of work completes successfully.
	/// </summary>
	/// <returns>A read-only collection of domain events.</returns>
	IReadOnlyCollection<IDomainEvent> GetDomainEvents();

	/// <summary>
	/// Clears all currently stored domain events from the aggregate.
	/// This is typically called after events have been successfully dispatched.
	/// </summary>
	void ClearDomainEvents();
}

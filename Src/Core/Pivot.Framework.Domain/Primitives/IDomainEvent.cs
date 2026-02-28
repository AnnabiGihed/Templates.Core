using MediatR;

namespace Pivot.Framework.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines the contract for a domain event.
///              A domain event represents a meaningful business occurrence within the domain model
///              and is published to notify interested handlers after state changes.
///              It integrates with MediatR to support in-process dispatching.
/// </summary>
public interface IDomainEvent : INotification
{
	/// <summary>
	/// Gets the unique identifier of the domain event instance.
	/// Used for traceability, idempotency, and event correlation.
	/// </summary>
	Guid Id { get; init; }

	/// <summary>
	/// Gets the UTC timestamp indicating when the domain event occurred.
	/// This value must always be expressed in UTC to ensure consistency across systems.
	/// </summary>
	DateTime OccurredOnUtc { get; init; }
}

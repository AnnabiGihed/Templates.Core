using Templates.Core.Domain.Primitives;

namespace Templates.Core.Domain.DomainEvents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base type for domain events.
///              A DomainEvent represents a meaningful business occurrence within the domain model.
///              It carries a unique identifier and the UTC timestamp of occurrence.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
	/// <summary>
	/// Initializes a new domain event with explicit identifier and occurrence timestamp.
	/// </summary>
	/// <param name="id">Unique identifier of the domain event instance.</param>
	/// <param name="occurredOnUtc">UTC timestamp indicating when the event occurred.</param>
	protected DomainEvent(Guid id, DateTime occurredOnUtc)
	{
		Id = id;
		OccurredOnUtc = occurredOnUtc;
	}

	/// <summary>
	/// Initializes a new domain event with a generated identifier and the current UTC timestamp.
	/// </summary>
	protected DomainEvent()
		: this(Guid.NewGuid(), DateTime.UtcNow)
	{
	}

	/// <summary>
	/// Gets the unique identifier of the domain event instance.
	/// </summary>
	public Guid Id { get; init; }

	/// <summary>
	/// Gets the UTC timestamp indicating when the domain event occurred.
	/// </summary>
	public DateTime OccurredOnUtc { get; init; }
}

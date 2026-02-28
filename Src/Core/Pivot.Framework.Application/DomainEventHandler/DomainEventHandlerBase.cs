using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Pivot.Framework.Application.Abstractions.Messaging.Events;

namespace Pivot.Framework.Application.DomainEventHandler;

public abstract class DomainEventHandlerBase<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
	/// <summary>
	/// Handles the domain event with a result.
	/// </summary>
	/// <param name="domainEvent">The domain event.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A result indicating success or failure.</returns>
	public abstract Task<Result> HandleWithResultAsync(TEvent domainEvent, CancellationToken cancellationToken);

	/// <summary>
	/// Mediator-compatible handler. Forwards to HandleWithResultAsync.
	/// </summary>
	/// <param name="notification">The notification.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A completed Task.</returns>
	public async Task Handle(TEvent notification, CancellationToken cancellationToken)
	{
		await HandleWithResultAsync(notification, cancellationToken);
	}
}
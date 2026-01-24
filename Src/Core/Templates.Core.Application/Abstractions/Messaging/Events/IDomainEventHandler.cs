using MediatR;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Application.Abstractions.Messaging.Events;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Contract for domain event handlers compatible with MediatR notifications,
///              while allowing handlers to return a <see cref="Result"/> for composability and diagnostics.
///              MediatR will invoke <see cref="INotificationHandler{TNotification}.Handle"/>,
///              which delegates to <see cref="HandleWithResultAsync"/>.
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
	where TEvent : IDomainEvent
{
	/// <summary>
	/// Handles the domain event and returns a <see cref="Result"/>.
	/// </summary>
	Task<Result> HandleWithResultAsync(TEvent domainEvent, CancellationToken cancellationToken);

	/// <summary>
	/// MediatR entry point. Delegates to <see cref="HandleWithResultAsync"/>.
	/// </summary>
	async Task INotificationHandler<TEvent>.Handle(TEvent notification, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(notification);

		// Result is intentionally ignored here to comply with MediatR's notification contract.
		// Your publishing infrastructure (if needed) can call HandleWithResultAsync directly.
		_ = await HandleWithResultAsync(notification, cancellationToken);
	}
}

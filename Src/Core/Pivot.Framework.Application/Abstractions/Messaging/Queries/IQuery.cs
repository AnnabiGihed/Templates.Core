using MediatR;
using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Abstractions.Messaging.Queries;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marker interface for query requests in the application layer.
///              Queries represent read-only operations and must not mutate state.
///              They return a <see cref="Result{TValue}"/> to standardize success/failure handling.
/// </summary>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

using MediatR;
using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Abstractions.Messaging.Commands;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marker interface for command requests in the application layer.
///              Commands represent intent to change system state.
///              They return a <see cref="Result"/> to standardize success/failure handling.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marker interface for command requests in the application layer that return a value.
///              Commands represent intent to change system state.
///              They return a <see cref="Result{TValue}"/> to standardize success/failure handling.
/// </summary>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

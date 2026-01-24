using MediatR;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Application.Abstractions.Messaging.Commands;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Contract for handling commands that do not return a payload.
///              Enforces the application's Result-based success/failure model.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
	where TCommand : ICommand
{
}

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Contract for handling commands that return a payload.
///              Enforces the application's Result-based success/failure model.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
	where TCommand : ICommand<TResponse>
{
}

using MediatR;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Application.Abstractions.Messaging.Queries;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Contract for handling queries (read-only operations) in the application layer.
///              Queries must not mutate state and return a <see cref="Result{TValue}"/> to standardize
///              success/failure handling.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
	where TQuery : IQuery<TResponse>
{
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Templates.Core.Domain.Shared;
using Templates.Core.Application.Exceptions;

namespace Templates.Core.Containers.API.Abstractions;

[ApiController]
public abstract class ApiController : ControllerBase
{
	protected readonly ISender Sender;

	protected ApiController(ISender sender) => Sender = sender;

	#region -Option 1 : HandleFailure

	protected ActionResult HandleFailure(Result result)
	{
		return (result.ResultExceptionType == ResultExceptionType.NotFound) ?
				HandleNotFoundFailure(result)
				:
				HandleBadRequestFailure(result);
	}

	private ActionResult HandleNotFoundFailure(Result result) =>
		result switch
		{
			{ IsSuccess: true } => throw new InvalidOperationException(),
			_ =>
				NotFound(
					CreateProblemDetails(
						"Not Found",
						StatusCodes.Status404NotFound,
						result.Error))
		};

	private ActionResult HandleBadRequestFailure(Result result) =>
		result switch
		{
			{ IsSuccess: true } => throw new InvalidOperationException(),
			IValidationResult validationResult =>
				BadRequest(
					CreateProblemDetails(
						"Validation Error", StatusCodes.Status400BadRequest,
						result.Error,
						validationResult.Errors)),
			_ =>
				BadRequest(
					CreateProblemDetails(
						"Bad Request",
						StatusCodes.Status400BadRequest,
						result.Error))
		};

	private static ProblemDetails CreateProblemDetails(
		string title,
		int status,
		Error error,
		Error[]? errors = null) =>
		new()
		{
			Title = title,
			Type = error.Code,
			Detail = error.Message,
			Status = status,
			Extensions = { { nameof(errors), errors } }
		};

	#endregion -Option 1 : HandleFailure

	#region -Option 2 : HandleGlobalFailure

	protected ActionResult HandleGlobalFailure(Result result)
	{
		return (result.ResultExceptionType == ResultExceptionType.NotFound) ?
				HandleGlobalNotFoundFailure(result)
				:
				HandleGlobalBadRequestFailure(result);
	}

	private ActionResult HandleGlobalNotFoundFailure(Result result) =>
		result switch
		{
			{ IsSuccess: true } => throw new InvalidOperationException(),
			_ =>
				throw new NotFoundException(result.Error.Code, result.Error.Message)
		};

	private ActionResult HandleGlobalBadRequestFailure(Result result) =>
		result switch
		{
			{ IsSuccess: true } => throw new InvalidOperationException(),
			IValidationResult validationResult =>
				throw new BadRequestException(result.Error, validationResult),
			_ =>
				throw new BadRequestException(result.Error)
		};

	#endregion -Option 2 : HandleGlobalFailure

	#region -Option 3 : HandleResult

	protected ActionResult HandleResult<T>(Result<T> result)
	{
		if (result.IsFailure)
		{
			return HandleFailure(result);
		}
		return Ok(result);
	}

	#endregion -Option 3 : HandleResult
}

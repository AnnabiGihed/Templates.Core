using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pivot.Framework.Application.Exceptions;
using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Containers.API.Abstractions;

[ApiController]
public abstract class ApiController : ControllerBase
{
	protected readonly ISender Sender;

	protected ApiController(ISender sender) =>
		Sender = sender ?? throw new ArgumentNullException(nameof(sender));

	#region Option 1 : HandleFailure (returns ProblemDetails directly)

	protected ActionResult HandleFailure(Result result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.IsSuccess)
			throw new InvalidOperationException("HandleFailure cannot be called for a successful result.");

		return result.ResultExceptionType switch
		{
			ResultExceptionType.NotFound => HandleNotFoundFailure(result),
			ResultExceptionType.Conflict => HandleConflictFailure(result),
			ResultExceptionType.Unauthorized => HandleUnauthorizedFailure(result),
			ResultExceptionType.Forbidden => HandleForbiddenFailure(result),
			_ => HandleBadRequestFailure(result)
		};
	}

	private ActionResult HandleNotFoundFailure(Result result) =>
		NotFound(CreateProblemDetails(
			title: "Not Found",
			status: StatusCodes.Status404NotFound,
			error: result.Error,
			validationErrors: (result as IValidationResult)?.Errors));

	private ActionResult HandleConflictFailure(Result result) =>
		Conflict(CreateProblemDetails(
			title: "Conflict",
			status: StatusCodes.Status409Conflict,
			error: result.Error,
			validationErrors: (result as IValidationResult)?.Errors));

	private ActionResult HandleUnauthorizedFailure(Result result) =>
		Unauthorized(CreateProblemDetails(
			title: "Unauthorized",
			status: StatusCodes.Status401Unauthorized,
			error: result.Error,
			validationErrors: (result as IValidationResult)?.Errors));

	private ActionResult HandleForbiddenFailure(Result result) =>
		StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(
			title: "Forbidden",
			status: StatusCodes.Status403Forbidden,
			error: result.Error,
			validationErrors: (result as IValidationResult)?.Errors));

	private ActionResult HandleBadRequestFailure(Result result) =>
		BadRequest(CreateProblemDetails(
			title: result is IValidationResult ? "Validation Error" : "Bad Request",
			status: StatusCodes.Status400BadRequest,
			error: result.Error,
			validationErrors: (result as IValidationResult)?.Errors));

	private static ProblemDetails CreateProblemDetails(
		string title,
		int status,
		Error error,
		IReadOnlyCollection<Error>? validationErrors = null)
	{
		ArgumentNullException.ThrowIfNull(error);

		var problem = new ProblemDetails
		{
			Title = title,
			Status = status,
			Type = error.Code,
			Detail = error.Message
		};

		if (validationErrors is not null && validationErrors.Count > 0)
			problem.Extensions[nameof(validationErrors)] = validationErrors;

		return problem;
	}

	#endregion

	#region Option 2 : HandleGlobalFailure (throws exceptions for middleware)

	protected ActionResult HandleGlobalFailure(Result result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.IsSuccess)
			throw new InvalidOperationException("HandleGlobalFailure cannot be called for a successful result.");

		// If you use global exception middleware, throw strongly typed exceptions here.
		return result.ResultExceptionType switch
		{
			ResultExceptionType.NotFound => throw CreateNotFoundException(result),
			ResultExceptionType.Conflict => throw new BadRequestException(result.Error), // or create a ConflictException if you want
			ResultExceptionType.Unauthorized => throw new BadRequestException(result.Error), // or UnauthorizedException
			ResultExceptionType.Forbidden => throw new BadRequestException(result.Error), // or ForbiddenException
			_ => throw CreateBadRequestException(result)
		};
	}

	private static Exception CreateNotFoundException(Result result)
	{
		// Your NotFoundException expects (name, key). We do not have a natural "key" here.
		// Best we can do is map name=error.Code and key=error.Message (or error.Code again).
		// Prefer a richer NotFoundException signature if you want strong typing.
		return new NotFoundException(result.Error.Code, result.Error.Message);
	}

	private static Exception CreateBadRequestException(Result result)
	{
		return result is IValidationResult validationResult
			? new BadRequestException(result.Error, validationResult)
			: new BadRequestException(result.Error);
	}

	#endregion

	#region Option 3 : HandleResult (generic helper for controllers)

	protected ActionResult HandleResult<T>(Result<T> result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.IsFailure)
			return HandleFailure(result);

		// Usually you return the value, not the Result wrapper.
		return Ok(result.Value);
	}

	#endregion
}

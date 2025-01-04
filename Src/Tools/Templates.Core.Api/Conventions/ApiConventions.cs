using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Templates.Core.Domain.Shared;
using System.Linq.Dynamic.Core;

namespace Templates.Core.Tools.API.Conventions;

public static class ApiConventions
{
	// Default for Result<T>
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void DefaultResult<T>() { }

	// Default for non-Result<T> generic responses
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void Default<T>() { }

	// Non-generic actions (e.g., void/Task/IActionResult)
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	public static void Delete() { }

	// Create actions (e.g., POST that returns Created)
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void Create<T>() { }

	// Paged results or custom response types
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	public static void PagedResult<T>() { }

	// Actions with only 200 and 400 responses
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void DefaultResultNoNotFound<T>() { }
}

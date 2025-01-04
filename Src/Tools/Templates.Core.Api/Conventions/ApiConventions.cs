using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Templates.Core.Tools.API.Conventions;
public static class ApiConventions
{
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void Default<T>() { }

	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	public static void DefaultNoNotFound<T>() { }
}
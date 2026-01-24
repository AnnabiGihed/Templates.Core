using Microsoft.AspNetCore.Mvc;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Containers.API.Middleware;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Helpers to enrich <see cref="ProblemDetails"/> with validation errors.
/// </summary>
public static class ProblemDetailsExtensions
{
	public static ProblemDetails WithValidationErrors(
		this ProblemDetails problemDetails,
		IReadOnlyCollection<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(problemDetails);
		ArgumentNullException.ThrowIfNull(errors);

		if (errors.Count > 0)
			problemDetails.Extensions["validationErrors"] = errors;

		return problemDetails;
	}
}

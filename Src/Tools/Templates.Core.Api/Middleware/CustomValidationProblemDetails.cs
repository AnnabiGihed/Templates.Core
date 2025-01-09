using Microsoft.AspNetCore.Mvc;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Containers.API.Middleware;

public class CustomValidationProblemDetails : ProblemDetails
{
	public Error[] ValidationErrors { get; set; } = default!;
}

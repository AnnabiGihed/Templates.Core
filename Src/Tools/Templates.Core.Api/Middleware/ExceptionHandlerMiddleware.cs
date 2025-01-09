using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Templates.Core.Application.Exceptions;

namespace Templates.Core.Containers.API.Middleware;

public class ExceptionHandlerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionHandlerMiddleware> _logger;

	public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task InvokeAsync(HttpContext httpContext)
	{
		try
		{
			await _next(httpContext);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(httpContext, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
	{
		HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
		CustomValidationProblemDetails problem = new();

		httpContext.Response.ContentType = "application/json";

		switch (ex)
		{
			case ValidationException validationException:
				statusCode = HttpStatusCode.BadRequest;
				problem = new CustomValidationProblemDetails
				{
					Title = validationException.Message,
					Status = (int)statusCode,
					Detail = validationException.InnerException?.Message,
					Type = nameof(ValidationException),
					ValidationErrors = validationException.ValidationErrors
				};
				break;
			case BadRequestException badRequestException:
				statusCode = HttpStatusCode.BadRequest;
				problem = new CustomValidationProblemDetails
				{
					Title = badRequestException.Message,
					Status = (int)statusCode,
					Detail = badRequestException.InnerException?.Message,
					Type = nameof(BadRequestException),
					ValidationErrors = badRequestException.ValidationErrors
				};
				break;
			case NotFoundException notFoundException:
				statusCode = HttpStatusCode.NotFound;
				problem = new CustomValidationProblemDetails
				{
					Title = notFoundException.Message,
					Status = (int)statusCode,
					Type = nameof(NotFoundException),
					Detail = notFoundException.InnerException?.Message
				};
				break;
			default:
				problem = new CustomValidationProblemDetails
				{
					Title = ex.Message,
					Status = (int)statusCode,
					Type = nameof(HttpStatusCode.InternalServerError),
					Detail = ex.StackTrace,
				};
				break;
		}

		httpContext.Response.StatusCode = (int)statusCode;

		// log the exception details (validation errors)
		var logMessage = JsonConvert.SerializeObject(problem);
		_logger.LogError(logMessage);

		await httpContext.Response.WriteAsJsonAsync(problem);
	}
}
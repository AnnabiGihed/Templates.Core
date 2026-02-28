using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Application-level exception representing an HTTP 400 Bad Request.
///              Typically thrown from API/application boundaries when a request cannot be processed
///              due to validation or client-side input issues.
///              Carries one or more validation errors for ProblemDetails / response payloads.
/// </summary>
public sealed class BadRequestException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BadRequestException"/> class from a single error.
	/// </summary>
	/// <param name="error">The primary error.</param>
	public BadRequestException(Error error)
		: base(BuildMessage(error))
	{
		PrimaryError = error ?? throw new ArgumentNullException(nameof(error));
		ValidationErrors = new[] { PrimaryError };
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BadRequestException"/> class from a primary error
	/// and a validation result containing multiple errors.
	/// </summary>
	/// <param name="error">The primary error.</param>
	/// <param name="validationResult">The validation result containing additional errors.</param>
	public BadRequestException(Error error, IValidationResult validationResult)
		: base(BuildMessage(error))
	{
		PrimaryError = error ?? throw new ArgumentNullException(nameof(error));
		ArgumentNullException.ThrowIfNull(validationResult);

		// Normalize and ensure at least the primary error is present.
		var errors = (validationResult.Errors ?? Array.Empty<Error>())
			.Where(e => e is not null && e != Error.None)
			.ToArray();

		ValidationErrors = errors.Length > 0 ? errors : new[] { PrimaryError };
	}

	/// <summary>
	/// Gets the primary error that triggered the bad request.
	/// </summary>
	public Error PrimaryError { get; }

	/// <summary>
	/// Gets all validation/client errors associated with the bad request.
	/// </summary>
	public IReadOnlyCollection<Error> ValidationErrors { get; }

	private static string BuildMessage(Error error)
	{
		ArgumentNullException.ThrowIfNull(error);

		// Prefer the human-readable message; fallback to code.
		return string.IsNullOrWhiteSpace(error.Message) ? error.Code : error.Message;
	}
}

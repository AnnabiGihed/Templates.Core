using Templates.Core.Domain.Shared;

namespace Templates.Core.Application.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Application-level exception representing a validation failure (HTTP 400).
///              Carries one or more validation errors that can be returned in a ProblemDetails response.
/// </summary>
public sealed class ValidationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationException"/> class from a single error.
	/// </summary>
	/// <param name="error">The primary validation error.</param>
	public ValidationException(Error error)
		: base(BuildMessage(error))
	{
		PrimaryError = error ?? throw new ArgumentNullException(nameof(error));
		ValidationErrors = new[] { PrimaryError };
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationException"/> class from a primary error
	/// and a validation result containing multiple errors.
	/// </summary>
	/// <param name="error">The primary validation error.</param>
	/// <param name="validationResult">The validation result containing additional errors.</param>
	public ValidationException(Error error, IValidationResult validationResult)
		: base(BuildMessage(error))
	{
		PrimaryError = error ?? throw new ArgumentNullException(nameof(error));
		ArgumentNullException.ThrowIfNull(validationResult);

		var errors = (validationResult.Errors ?? Array.Empty<Error>())
			.Where(e => e is not null && e != Error.None)
			.ToArray();

		ValidationErrors = errors.Length > 0 ? errors : new[] { PrimaryError };
	}

	/// <summary>
	/// Gets the primary validation error that triggered the exception.
	/// </summary>
	public Error PrimaryError { get; }

	/// <summary>
	/// Gets all validation errors associated with the exception.
	/// </summary>
	public IReadOnlyCollection<Error> ValidationErrors { get; }

	private static string BuildMessage(Error error)
	{
		ArgumentNullException.ThrowIfNull(error);
		return string.IsNullOrWhiteSpace(error.Message) ? error.Code : error.Message;
	}
}

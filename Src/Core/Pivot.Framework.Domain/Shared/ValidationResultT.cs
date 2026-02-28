using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a failure result caused by validation errors.
///              Provides a list of validation errors while keeping the Result/Error contract.
/// </summary>
/// <typeparam name="TValue">The result value type.</typeparam>
public sealed class ValidationResult<TValue> : Result<TValue>, IValidationResult
{
	private readonly IReadOnlyCollection<Error> _errors;

	private ValidationResult(IReadOnlyCollection<Error> errors)
		: base(
			value: default,
			isSuccess: false,
			error: ValidationErrors.ValidationError,
			resultExceptionType: ResultExceptionType.BadRequest)
	{
		_errors = errors;
	}

	/// <summary>
	/// Gets the validation errors that caused the failure.
	/// </summary>
	public IReadOnlyCollection<Error> Errors => _errors;

	/// <summary>
	/// Creates a validation result with the provided errors.
	/// </summary>
	/// <param name="errors">Validation errors.</param>
	/// <returns>A failure validation result.</returns>
	public static ValidationResult<TValue> WithErrors(IEnumerable<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(errors);

		var list = errors.Where(e => e is not null && e != Error.None).ToArray();
		if (list.Length == 0)
			throw new ArgumentException("At least one validation error must be provided.", nameof(errors));

		return new ValidationResult<TValue>(list);
	}

	/// <summary>
	/// Convenience overload for array inputs.
	/// </summary>
	public static ValidationResult<TValue> WithErrors(params Error[] errors)
		=> WithErrors((IEnumerable<Error>)errors);
}

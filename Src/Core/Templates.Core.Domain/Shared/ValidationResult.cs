namespace Templates.Core.Domain.Shared;


/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a failure result caused by validation errors (non-generic).
/// </summary>
public sealed class ValidationResult : Result, IValidationResult
{
	private readonly IReadOnlyCollection<Error> _errors;

	private ValidationResult(IReadOnlyCollection<Error> errors)
		: base(false, ValidationErrors.ValidationError, ResultExceptionType.BadRequest)
	{
		_errors = errors;
	}

	public IReadOnlyCollection<Error> Errors => _errors;

	public static ValidationResult WithErrors(IEnumerable<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(errors);

		var list = errors.Where(e => e is not null && e != Error.None).ToArray();
		if (list.Length == 0)
			throw new ArgumentException("At least one validation error must be provided.", nameof(errors));

		return new ValidationResult(list);
	}

	public static ValidationResult WithErrors(params Error[] errors) => WithErrors((IEnumerable<Error>)errors);
}

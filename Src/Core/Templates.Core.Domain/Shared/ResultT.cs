namespace Templates.Core.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents the outcome of an operation returning a value.
///              On success, the value is available.
///              On failure, accessing <see cref="Value"/> throws.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
public class Result<TValue> : Result
{
	private readonly TValue? _value;

	/// <summary>
	/// Initializes a new instance of the <see cref="Result{TValue}"/> class.
	/// </summary>
	/// <param name="value">The result value (required when success).</param>
	/// <param name="isSuccess">Whether the result represents success.</param>
	/// <param name="error">The error for failure results; <see cref="Error.None"/> for success results.</param>
	/// <param name="resultExceptionType">Classification used for mapping to transport semantics (e.g., HTTP status).</param>
	protected internal Result(
		TValue? value,
		bool isSuccess,
		Error error,
		ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
		: base(isSuccess, error, resultExceptionType)
	{
		// Enforce consistency with Value getter semantics:
		// - Success must carry a value (non-null)
		// - Failure can carry default/null
		if (isSuccess && value is null)
			throw new InvalidOperationException("A successful Result<TValue> must have a non-null value.");

		_value = value;
	}

	/// <summary>
	/// Gets the value of a successful result.
	/// Accessing this property on a failure result throws.
	/// </summary>
	public TValue Value => IsSuccess
		? _value!
		: throw new InvalidOperationException("The value of a failure result cannot be accessed.");

	/// <summary>
	/// Implicitly converts a value into a <see cref="Result{TValue}"/> using <see cref="Result.Create{TValue}(TValue?)"/>.
	/// If the value is null, the result is a failure with <see cref="Error.NullValue"/>.
	/// </summary>
	public static implicit operator Result<TValue>(TValue? value) => Result.Create(value);
}

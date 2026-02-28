namespace Pivot.Framework.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents the outcome of an operation.
///              - Success results carry no error.
///              - Failure results carry an error.
///              A ResultExceptionType is included to help API layers map failures to HTTP semantics.
/// </summary>
public class Result
{
	protected internal Result(
		bool isSuccess,
		Error error,
		ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
	{
		ArgumentNullException.ThrowIfNull(error);

		if (isSuccess && error != Error.None)
			throw new InvalidOperationException("A successful result cannot contain an error.");

		if (!isSuccess && error == Error.None)
			throw new InvalidOperationException("A failure result must contain an error.");

		// Ensure success always has None semantics
		ResultExceptionType = isSuccess ? ResultExceptionType.None : resultExceptionType;

		IsSuccess = isSuccess;
		Error = error;
	}

	/// <summary>
	/// Gets whether the result indicates success.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets whether the result indicates failure.
	/// </summary>
	public bool IsFailure => !IsSuccess;

	/// <summary>
	/// Gets the error for failure results; <see cref="Error.None"/> for success results.
	/// </summary>
	public Error Error { get; }

	/// <summary>
	/// Gets the exception type classification used for mapping to transport semantics (e.g., HTTP status).
	/// </summary>
	public ResultExceptionType ResultExceptionType { get; }

	/// <summary>
	/// Creates a success result.
	/// </summary>
	public static Result Success() => new(true, Error.None, ResultExceptionType.None);

	/// <summary>
	/// Creates a success result carrying a value.
	/// </summary>
	public static Result<TValue> Success<TValue>(TValue value)
		=> new(value, true, Error.None, ResultExceptionType.None);

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	public static Result Failure(Error error, ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
		=> new(false, error, resultExceptionType);

	/// <summary>
	/// Creates a failure result carrying a value type default.
	/// </summary>
	public static Result<TValue> Failure<TValue>(Error error, ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
		=> new(default, false, error, resultExceptionType);

	/// <summary>
	/// Creates a result from a nullable value.
	/// Returns success when value is not null; otherwise returns failure with <see cref="Error.NullValue"/>.
	/// </summary>
	public static Result<TValue> Create<TValue>(TValue? value)
		=> value is not null ? Success(value) : Failure<TValue>(Error.NullValue, ResultExceptionType.BadRequest);
}

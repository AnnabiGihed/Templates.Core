using Templates.Core.Domain.Shared;

namespace Templates.Core.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Extension methods to compose <see cref="Result{TValue}"/> operations
///              without exceptions (functional style).
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Ensures that a successful result satisfies a predicate.
	/// If the predicate fails, returns a failure result with the provided error.
	/// If the result is already a failure, it is returned unchanged.
	/// </summary>
	public static Result<T> Ensure<T>(
		this Result<T> result,
		Func<T, bool> predicate,
		Error error,
		ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
	{
		ArgumentNullException.ThrowIfNull(result);
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(error);

		if (result.IsFailure)
			return result;

		return predicate(result.Value)
			? result
			: Result.Failure<T>(error, resultExceptionType);
	}

	/// <summary>
	/// Maps a successful result value into a new result.
	/// Failures are propagated including their error and exception type.
	/// </summary>
	public static Result<TOut> Map<TIn, TOut>(
		this Result<TIn> result,
		Func<TIn, TOut> mappingFunc)
	{
		ArgumentNullException.ThrowIfNull(result);
		ArgumentNullException.ThrowIfNull(mappingFunc);

		if (result.IsFailure)
			return Result.Failure<TOut>(result.Error, result.ResultExceptionType);

		// mappingFunc may throw; that is acceptable unless you want a SafeMap.
		return Result.Success(mappingFunc(result.Value));
	}

	/// <summary>
	/// Maps a successful result value into a new result and captures mapping exceptions
	/// as a failure result.
	/// </summary>
	public static Result<TOut> SafeMap<TIn, TOut>(
		this Result<TIn> result,
		Func<TIn, TOut> mappingFunc,
		Func<Exception, Error>? errorFactory = null,
		ResultExceptionType resultExceptionType = ResultExceptionType.BadRequest)
	{
		ArgumentNullException.ThrowIfNull(result);
		ArgumentNullException.ThrowIfNull(mappingFunc);

		if (result.IsFailure)
			return Result.Failure<TOut>(result.Error, result.ResultExceptionType);

		try
		{
			return Result.Success(mappingFunc(result.Value));
		}
		catch (Exception ex)
		{
			var error = errorFactory?.Invoke(ex) ?? Error.SystemError(ex.Message);
			return Result.Failure<TOut>(error, resultExceptionType);
		}
	}
}

using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Containers.API.Abstractions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Standard API response wrapper.
///              Provides a consistent envelope containing:
///              - success flag
///              - message
///              - optional validation errors
///              - data payload
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public sealed class ApiResponse<T>
{
	/// <summary>
	/// Indicates whether the request was successful.
	/// </summary>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Human-readable message (optional).
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Validation / business errors associated with the response.
	/// Empty when <see cref="Success"/> is true.
	/// </summary>
	public IReadOnlyCollection<Error> Errors { get; init; } = Array.Empty<Error>();

	/// <summary>
	/// Response payload. Only expected to be populated when <see cref="Success"/> is true.
	/// </summary>
	public T? Data { get; init; }

	/// <summary>
	/// Creates a successful response.
	/// </summary>
	public static ApiResponse<T> Ok(T data, string? message = null) =>
		new()
		{
			Success = true,
			Message = message ?? string.Empty,
			Data = data,
			Errors = Array.Empty<Error>()
		};

	/// <summary>
	/// Creates a failed response.
	/// </summary>
	public static ApiResponse<T> Fail(Error error, IEnumerable<Error>? errors = null, string? message = null) =>
		new()
		{
			Success = false,
			Message = message ?? (string.IsNullOrWhiteSpace(error.Message) ? error.Code : error.Message),
			Data = default,
			Errors = NormalizeErrors(error, errors)
		};

	/// <summary>
	/// Creates a response from a <see cref="Result{TValue}"/>.
	/// </summary>
	public static ApiResponse<T> FromResult(Result<T> result, string? successMessage = null)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.IsSuccess)
			return Ok(result.Value, successMessage);

		var validationErrors = (result as IValidationResult)?.Errors;
		return Fail(result.Error, validationErrors, result.Error.Message);
	}

	private static IReadOnlyCollection<Error> NormalizeErrors(Error primary, IEnumerable<Error>? extra)
	{
		var list = new List<Error>();

		if (primary is not null && primary != Error.None)
			list.Add(primary);

		if (extra is not null)
			list.AddRange(extra.Where(e => e is not null && e != Error.None));

		return list;
	}
}

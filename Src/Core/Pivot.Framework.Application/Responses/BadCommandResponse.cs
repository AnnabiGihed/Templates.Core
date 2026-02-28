using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Legacy command response DTO.
///              Kept for backward compatibility with consumers not yet migrated to Result-based responses.
/// </summary>
public sealed class BaseCommandResponse
{
	public bool Success { get; }
	public string Message { get; }
	public IReadOnlyCollection<string> ValidationErrors { get; }

	private BaseCommandResponse(bool success, string message, IEnumerable<string>? validationErrors = null)
	{
		Success = success;
		Message = message;
		ValidationErrors = validationErrors?.ToArray() ?? Array.Empty<string>();
	}

	public static BaseCommandResponse Ok(string? message = null)
		=> new(true, message ?? string.Empty);

	public static BaseCommandResponse Fail(
		string message,
		IEnumerable<Error>? errors = null)
	{
		var validationErrors = errors?
			.Where(e => e is not null && e != Error.None)
			.Select(e => e.Message)
			.ToArray();

		return new BaseCommandResponse(false, message, validationErrors);
	}
}

namespace Templates.Core.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Standard validation-related errors.
/// </summary>
public static class ValidationErrors
{
	public static readonly Error ValidationError =
		new("ValidationError", "A validation problem occurred.");
}

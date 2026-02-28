namespace Pivot.Framework.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents the result of a validation operation.
///              Used to expose multiple validation errors without throwing exceptions.
/// </summary>
public interface IValidationResult
{
	/// <summary>
	/// Gets the collection of validation errors.
	/// </summary>
	IReadOnlyCollection<Error> Errors { get; }
}

namespace Pivot.Framework.Domain.Shared;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Classification used by application/API layers to map <see cref="Result"/> failures
///              to transport semantics (e.g., HTTP status codes).
/// </summary>
public enum ResultExceptionType
{
	/// <summary>
	/// No exception type. Used for successful results.
	/// </summary>
	None = 0,

	/// <summary>
	/// Client sent invalid data (HTTP 400).
	/// </summary>
	BadRequest = 1,

	/// <summary>
	/// Resource not found (HTTP 404).
	/// </summary>
	NotFound = 2,

	/// <summary>
	/// Resource already exists / concurrency conflict (HTTP 409).
	/// </summary>
	Conflict = 3,

	/// <summary>
	/// Authentication required/failed (HTTP 401).
	/// </summary>
	Unauthorized = 4,

	/// <summary>
	/// Authenticated but not allowed (HTTP 403).
	/// </summary>
	Forbidden = 5
}

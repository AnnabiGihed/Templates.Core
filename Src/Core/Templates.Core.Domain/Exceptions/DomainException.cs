namespace Templates.Core.Domain.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base exception type for domain validation failures.
///              Inherits from <see cref="ArgumentException"/> to preserve parameter metadata.
/// </summary>
public abstract class DomainException : ArgumentException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DomainException"/> class.
	/// </summary>
	/// <param name="parameterName">The name of the parameter that caused the exception.</param>
	/// <param name="message">The exception message.</param>
	protected DomainException(string parameterName, string message)
		: base(message, parameterName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
		ArgumentNullException.ThrowIfNull(message);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
	/// </summary>
	/// <param name="parameterName">The name of the parameter that caused the exception.</param>
	/// <param name="message">The exception message.</param>
	/// <param name="innerException">The inner exception.</param>
	protected DomainException(string parameterName, string message, Exception innerException)
		: base(message, parameterName, innerException)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
		ArgumentNullException.ThrowIfNull(message);
		ArgumentNullException.ThrowIfNull(innerException);
	}
}

using Templates.Core.Domain;
using Templates.Core.Domain.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Domain exception thrown when a value is outside an expected range.
/// </summary>
public sealed class OutOfRangeDomainException : DomainException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OutOfRangeDomainException"/> class.
	/// </summary>
	public OutOfRangeDomainException(string parameterName, string message)
		: base(parameterName, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OutOfRangeDomainException"/> class using the default resource message.
	/// </summary>
	public OutOfRangeDomainException(string parameterName)
		: this(parameterName, Resource.OutOfRange)
	{
	}
}
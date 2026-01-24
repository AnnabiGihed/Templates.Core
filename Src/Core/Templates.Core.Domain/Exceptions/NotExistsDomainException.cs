using Templates.Core.Domain;
using Templates.Core.Domain.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Domain exception thrown when an expected entity/value does not exist.
/// </summary>
public sealed class NotExistsDomainException : DomainException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NotExistsDomainException"/> class.
	/// </summary>
	public NotExistsDomainException(string parameterName, string message)
		: base(parameterName, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NotExistsDomainException"/> class using the default resource message.
	/// </summary>
	public NotExistsDomainException(string parameterName)
		: this(parameterName, Resource.NotExists)
	{
	}
}

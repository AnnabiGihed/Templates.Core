namespace Templates.Core.Domain.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Domain exception thrown when at least one value is required but none were provided.
/// </summary>
public sealed class AtLeastOneIsRequiredDomainException : DomainException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AtLeastOneIsRequiredDomainException"/> class.
	/// </summary>
	public AtLeastOneIsRequiredDomainException(string parameterName, string message)
		: base(parameterName, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AtLeastOneIsRequiredDomainException"/> class using the default resource message.
	/// </summary>
	public AtLeastOneIsRequiredDomainException(string parameterName)
		: this(parameterName, Resource.AtLeastOneIsRequired)
	{
	}
}
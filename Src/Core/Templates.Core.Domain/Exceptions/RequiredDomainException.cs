namespace Templates.Core.Domain.Exceptions;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Domain exception thrown when a required value is missing.
/// </summary>
public sealed class RequiredDomainException : DomainException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RequiredDomainException"/> class.
	/// </summary>
	public RequiredDomainException(string parameterName, string message)
		: base(parameterName, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RequiredDomainException"/> class using the default resource message.
	/// </summary>
	public RequiredDomainException(string parameterName)
		: this(parameterName, Resource.Required)
	{
	}
}
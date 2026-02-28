namespace Pivot.Framework.Domain.Exceptions;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Domain exception thrown when an unexpected/unknown domain error occurs.
/// </summary>
public sealed class UnknownDomainException : DomainException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UnknownDomainException"/> class.
	/// </summary>
	public UnknownDomainException(string parameterName, string message)
		: base(parameterName, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UnknownDomainException"/> class using the default resource message.
	/// </summary>
	public UnknownDomainException(string parameterName)
		: this(parameterName, Resource.Unknown)
	{
	}
}
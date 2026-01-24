namespace Templates.Core.Domain.Exceptions;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Utility scope that collects <see cref="DomainException"/> instances and throws a single
///              <see cref="AggregateDomainException"/> when disposed or when <see cref="ThrowIfAny"/> is called.
///              Intended for validation scenarios where multiple errors must be reported at once.
/// </summary>
public sealed class DomainExceptionScope : IDisposable
{
	private readonly List<DomainException> _domainExceptions = new();

	/// <summary>
	/// Adds a domain exception to the scope.
	/// </summary>
	/// <param name="domainException">The exception to add.</param>
	public void AddException(DomainException domainException)
	{
		ArgumentNullException.ThrowIfNull(domainException);
		_domainExceptions.Add(domainException);
	}

	/// <summary>
	/// Adds multiple domain exceptions to the scope.
	/// </summary>
	/// <param name="domainExceptions">Exceptions to add.</param>
	public void AddExceptions(IEnumerable<DomainException> domainExceptions)
	{
		ArgumentNullException.ThrowIfNull(domainExceptions);
		_domainExceptions.AddRange(domainExceptions.Where(e => e is not null));
	}

	/// <summary>
	/// Throws an <see cref="AggregateDomainException"/> if any exceptions were collected.
	/// </summary>
	public void ThrowIfAny()
	{
		if (_domainExceptions.Count == 0)
			return;

		var aggregateException = new AggregateDomainException(_domainExceptions);
		_domainExceptions.Clear();
		throw aggregateException;
	}

	/// <summary>
	/// Disposes the scope and throws if any exceptions were collected.
	/// </summary>
	public void Dispose()
	{
		try
		{
			ThrowIfAny();
		}
		finally
		{
			GC.SuppressFinalize(this);
		}
	}
}
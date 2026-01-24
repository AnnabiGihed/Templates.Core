using System.Collections.ObjectModel;

namespace Templates.Core.Domain.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents an aggregation of multiple <see cref="DomainException"/> instances.
///              Useful when validating multiple parameters and reporting all failures at once.
/// </summary>
public sealed class AggregateDomainException : AggregateException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AggregateDomainException"/> class.
	/// </summary>
	/// <param name="exceptions">The domain exceptions to aggregate.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="exceptions"/> is null.</exception>
	public AggregateDomainException(IEnumerable<DomainException> exceptions)
		: base(BuildMessage(exceptions), exceptions)
	{
		DomainExceptions = new ReadOnlyCollection<DomainException>(
			(exceptions ?? throw new ArgumentNullException(nameof(exceptions))).ToList());
	}

	/// <summary>
	/// Gets the aggregated domain exceptions as a strongly-typed read-only collection.
	/// </summary>
	public IReadOnlyCollection<DomainException> DomainExceptions { get; }

	private static string BuildMessage(IEnumerable<DomainException> exceptions)
	{
		if (exceptions is null) throw new ArgumentNullException(nameof(exceptions));

		var list = exceptions.ToList();
		return list.Count switch
		{
			0 => "One or more domain errors occurred.",
			1 => list[0].Message,
			_ => $"{list.Count} domain errors occurred."
		};
	}
}
namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base type for GUID-backed strongly-typed identifiers.
///              Provides:
///              - Non-empty GUID invariant
///              - Comparable semantics (by underlying GUID)
///              - Consistent string formatting
/// </summary>
/// <typeparam name="TSelf">The derived strongly-typed id type.</typeparam>
public abstract record StronglyTypedGuidId<TSelf> : IStronglyTypedId<TSelf>
	where TSelf : StronglyTypedGuidId<TSelf>
{
	/// <summary>
	/// Gets the underlying GUID value.
	/// </summary>
	public Guid Value { get; }

	/// <summary>
	/// Initializes a new strongly-typed GUID identifier.
	/// </summary>
	/// <param name="value">The underlying GUID value.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
	protected StronglyTypedGuidId(Guid value)
	{
		if (value == Guid.Empty)
			throw new ArgumentException("Identifier value cannot be Guid.Empty.", nameof(value));

		Value = value;
	}

	/// <summary>
	/// Returns the underlying GUID as a string.
	/// </summary>
	public override string ToString() => Value.ToString();

	/// <summary>
	/// Compares this identifier to another identifier instance by comparing the underlying GUIDs.
	/// </summary>
	public int CompareTo(TSelf? other)
	{
		if (other is null)
			return 1;

		return Value.CompareTo(other.Value);
	}
}

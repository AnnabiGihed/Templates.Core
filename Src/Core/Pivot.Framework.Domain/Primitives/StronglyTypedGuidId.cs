namespace Pivot.Framework.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base type for GUID-backed strongly-typed entity identifiers.
///              Every aggregate and entity defines its own sealed record that inherits from this type,
///              ensuring that different aggregate IDs are never accidentally interchangeable.
///              Provides a non-empty GUID guard, comparable semantics, and consistent string formatting.
/// </summary>
/// <typeparam name="TSelf">The concrete derived identifier type (CRTP pattern).</typeparam>
public abstract record StronglyTypedGuidId<TSelf> : IStronglyTypedId<TSelf>
	where TSelf : StronglyTypedGuidId<TSelf>
{
	#region Properties
	/// <summary>
	/// Gets the underlying <see cref="Guid"/> value of this identifier.
	/// </summary>
	public Guid Value { get; }
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new strongly-typed GUID identifier.
	/// </summary>
	/// <param name="value">The underlying GUID value. Must not be <see cref="Guid.Empty"/>.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
	protected StronglyTypedGuidId(Guid value)
	{
		if (value == Guid.Empty)
			throw new ArgumentException("Identifier value cannot be Guid.Empty.", nameof(value));

		Value = value;
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the underlying GUID formatted as a lowercase hyphenated string (e.g. <c>3f2504e0-4f89-11d3-9a0c-0305e82c3301</c>).
	/// </summary>
	/// <returns>The string representation of the underlying GUID.</returns>
	public override string ToString()
	{
		return Value.ToString();
	}
	#endregion

	#region Comparison
	/// <summary>
	/// Compares this identifier to another of the same concrete type by comparing the underlying GUIDs.
	/// </summary>
	/// <param name="other">The other identifier to compare against. May be <c>null</c>.</param>
	/// <returns>
	/// A positive integer when this identifier is greater than <paramref name="other"/>;
	/// zero when equal; a negative integer when less. A null <paramref name="other"/> is always less.
	/// </returns>
	public int CompareTo(TSelf? other)
	{
		if (other is null)
			return 1;

		return Value.CompareTo(other.Value);
	}
	#endregion
}
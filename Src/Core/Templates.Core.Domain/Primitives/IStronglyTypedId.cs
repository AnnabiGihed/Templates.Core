namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Non-generic marker interface for strongly-typed identifiers.
///              Useful for reflection and generic handling without knowing TSelf at compile time.
/// </summary>
public interface IStronglyTypedId
{
}

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Contract for strongly-typed identifiers used across the domain model.
///              All identifiers are comparable to enable consistent ordering/sorting.
/// </summary>
/// <typeparam name="TSelf">The strongly-typed id type.</typeparam>
public interface IStronglyTypedId<TSelf> : IStronglyTypedId, IComparable<TSelf>
{
}

namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for projection roots (read models).
/// </summary>
/// <typeparam name="TId">The identifier type of the projection.</typeparam>
public abstract class ProjectionRoot<TId>
	where TId : IStronglyTypedId<TId>
{
	public virtual TId Id { get; protected set; }

	protected ProjectionRoot(TId id)
	{
		Id = id ?? throw new ArgumentNullException(nameof(id));
	}

	protected ProjectionRoot()
	{
		Id = default!;
	}
}

namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines soft delete behavior for entities.
///              Exposes read-only deletion metadata and controlled mutation methods.
/// </summary>
public interface ISoftDeletableEntity
{
	/// <summary>
	/// Gets whether the entity is soft deleted.
	/// </summary>
	bool IsDeleted { get; }

	/// <summary>
	/// Gets the UTC timestamp when the entity was soft deleted.
	/// </summary>
	DateTime? DeletedOnUtc { get; }

	/// <summary>
	/// Gets the actor who soft deleted the entity.
	/// </summary>
	string? DeletedBy { get; }

	/// <summary>
	/// Marks the entity as soft deleted.
	/// Intended to be called by domain intent methods or infrastructure pipelines.
	/// </summary>
	void MarkDeleted(DateTime deletedOnUtc, string deletedBy);

	/// <summary>
	/// Restores a soft deleted entity.
	/// </summary>
	void MarkRestored(DateTime restoredOnUtc, string restoredBy);
}

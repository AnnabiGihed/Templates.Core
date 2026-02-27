namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines the soft-delete contract for domain entities.
///              Implementing entities are never physically removed from the database;
///              instead they are flagged as deleted and hidden from standard queries.
///              Exposes read-only deletion metadata and controlled mutation methods
///              so that the soft-delete state is only changed through explicit domain intent.
/// </summary>
public interface ISoftDeletableEntity
{
	/// <summary>
	/// Gets a value indicating whether this entity has been soft-deleted.
	/// Soft-deleted entities are excluded from standard repository queries by default.
	/// </summary>
	bool IsDeleted { get; }

	/// <summary>
	/// Gets the identifier of the actor who soft-deleted this entity.
	/// <c>null</c> when the entity has never been deleted.
	/// </summary>
	string? DeletedBy { get; }

	/// <summary>
	/// Gets the UTC timestamp at which this entity was soft-deleted.
	/// <c>null</c> when the entity has never been deleted.
	/// </summary>
	DateTime? DeletedOnUtc { get; }

	/// <summary>
	/// Marks this entity as soft-deleted.
	/// Intended to be called from domain intent methods or by the repository delete pipeline —
	/// never invoked directly from application or API code.
	/// </summary>
	/// <param name="deletedOnUtc">UTC timestamp of deletion.</param>
	/// <param name="deletedBy">Identifier of the actor performing the deletion.</param>
	void MarkDeleted(DateTime deletedOnUtc, string deletedBy);

	/// <summary>
	/// Restores a previously soft-deleted entity.
	/// Intended to be called from domain intent methods or by an administrative pipeline.
	/// </summary>
	/// <param name="restoredOnUtc">UTC timestamp of restoration.</param>
	/// <param name="restoredBy">Identifier of the actor performing the restoration.</param>
	void MarkRestored(DateTime restoredOnUtc, string restoredBy);
}
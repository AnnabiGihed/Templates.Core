namespace Pivot.Framework.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marks a domain entity as auditable.
///              Auditable entities expose an <see cref="AuditInfo"/> value object that tracks
///              creation and last-modification metadata (who and when).
///              The persistence pipeline reads this interface to stamp audit fields automatically
///              on every <c>SaveChanges</c> call.
/// </summary>
public interface IAuditableEntity
{
	/// <summary>
	/// Gets the audit metadata associated with this entity.
	/// Contains creation and last-modification timestamps and actor identifiers.
	/// </summary>
	AuditInfo Audit { get; }

	/// <summary>
	/// Sets the audit metadata.
	/// Intended exclusively for use by the persistence infrastructure during entity initialisation
	/// or EF Core materialisation — never called from domain code.
	/// </summary>
	/// <param name="audit">The <see cref="AuditInfo"/> value object to assign.</param>
	void SetAudit(AuditInfo audit);
}
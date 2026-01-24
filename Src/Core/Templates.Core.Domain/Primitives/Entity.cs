namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for domain entities used across solutions.
///              Provides:
///              - Identity-based equality semantics
///              - Audit metadata (<see cref="AuditInfo"/>)
///              - Soft delete support by default
/// </summary>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public abstract class Entity<TId> : IAuditableEntity, ISoftDeletableEntity
	where TId : IStronglyTypedId<TId>
{
	public virtual TId Id { get; protected set; }

	public virtual AuditInfo Audit { get; protected set; } = default!;

	public virtual bool IsDeleted { get; protected set; }
	public virtual DateTime? DeletedOnUtc { get; protected set; }
	public virtual string? DeletedBy { get; protected set; }

	protected Entity(TId id)
	{
		Id = id ?? throw new ArgumentNullException(nameof(id));
	}

	protected Entity()
	{
		Id = default!;
	}

	/// <summary>
	/// Explicit interface implementation to keep the domain setter controlled
	/// while allowing infrastructure to initialize audit.
	/// </summary>
	void IAuditableEntity.SetAudit(AuditInfo audit)
	{
		ArgumentNullException.ThrowIfNull(audit);
		Audit = audit;
	}


	protected void InitializeAudit(DateTime createdOnUtc, string createdBy)
	{
		Audit = AuditInfo.Create(createdOnUtc, createdBy);
	}

	protected void Touch(DateTime modifiedOnUtc, string modifiedBy)
	{
		EnsureAuditInitialized();
		Audit.Modify(modifiedOnUtc, modifiedBy);
	}

	// -----------------------
	// Soft delete: explicit interface implementation
	// -----------------------
	void ISoftDeletableEntity.MarkDeleted(DateTime deletedOnUtc, string deletedBy)
	{
		SoftDelete(deletedOnUtc, deletedBy);
	}

	void ISoftDeletableEntity.MarkRestored(DateTime restoredOnUtc, string restoredBy)
	{
		Restore(restoredOnUtc, restoredBy);
	}

	/// <summary>
	/// Soft deletes the entity (protected domain behavior).
	/// </summary>
	protected void SoftDelete(DateTime deletedOnUtc, string deletedBy)
	{
		if (IsDeleted)
			return;

		ValidateUtcDate(deletedOnUtc, nameof(deletedOnUtc));
		ValidateActor(deletedBy, nameof(deletedBy));

		IsDeleted = true;
		DeletedOnUtc = deletedOnUtc;
		DeletedBy = deletedBy;

		// Keep audit consistent
		if (Audit is not null)
			Audit.Modify(deletedOnUtc, deletedBy);
	}

	/// <summary>
	/// Restores a previously soft deleted entity (protected domain behavior).
	/// </summary>
	protected void Restore(DateTime restoredOnUtc, string restoredBy)
	{
		if (!IsDeleted)
			return;

		ValidateUtcDate(restoredOnUtc, nameof(restoredOnUtc));
		ValidateActor(restoredBy, nameof(restoredBy));

		IsDeleted = false;
		DeletedOnUtc = null;
		DeletedBy = null;

		if (Audit is not null)
			Audit.Modify(restoredOnUtc, restoredBy);
	}

	protected void SetAudit(AuditInfo audit)
	{
		ArgumentNullException.ThrowIfNull(audit);
		Audit = audit;
	}

	public override bool Equals(object? obj)
	{
		if (obj is null) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;

		return obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(GetType(), Id);
	}

	public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
	public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

	private void EnsureAuditInitialized()
	{
		if (Audit is null)
			throw new InvalidOperationException("AuditInfo must be initialized before performing audit updates. Call InitializeAudit() during creation or ensure the persistence layer materializes Audit.");
	}

	private static void ValidateActor(string actor, string paramName)
	{
		if (string.IsNullOrWhiteSpace(actor))
			throw new ArgumentNullException(paramName);
	}

	private static void ValidateUtcDate(DateTime dateUtc, string paramName)
	{
		if (dateUtc == DateTime.MaxValue || dateUtc == DateTime.MinValue)
			throw new ArgumentException("Date must be a valid value.", paramName);

		if (dateUtc.Kind != DateTimeKind.Utc)
			throw new ArgumentException("Date must be expressed in UTC (DateTimeKind.Utc).", paramName);
	}
}

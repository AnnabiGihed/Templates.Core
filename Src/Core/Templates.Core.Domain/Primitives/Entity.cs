namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base class for all domain entities.
///              Provides identity-based equality semantics, audit metadata via <see cref="AuditInfo"/>,
///              and soft-delete support through <see cref="ISoftDeletableEntity"/>.
///              All derived entity identifiers must implement <see cref="IStronglyTypedId{TId}"/>.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier type for this entity.</typeparam>
public abstract class Entity<TId> : IAuditableEntity, ISoftDeletableEntity
	where TId : IStronglyTypedId<TId>
{
	#region Properties
	/// <summary>
	/// Gets the unique identifier of this entity.
	/// </summary>
	public virtual TId Id { get; protected set; }

	/// <summary>
	/// Gets a value indicating whether this entity has been soft-deleted.
	/// </summary>
	public virtual bool IsDeleted { get; protected set; }

	/// <summary>
	/// Gets the actor who soft-deleted this entity, if applicable.
	/// </summary>
	public virtual string? DeletedBy { get; protected set; }

	/// <summary>
	/// Gets the UTC timestamp at which this entity was soft-deleted, if applicable.
	/// </summary>
	public virtual DateTime? DeletedOnUtc { get; protected set; }

	/// <summary>
	/// Gets the audit metadata (created/modified timestamps and actors) for this entity.
	/// Initialised by the factory on creation; updated by <see cref="Touch"/>.
	/// </summary>
	public virtual AuditInfo Audit { get; protected set; } = default!;
	#endregion

	#region Constructors
	/// <summary>
	/// Initialises a new <see cref="Entity{TId}"/> with the specified identity.
	/// </summary>
	/// <param name="id">The strongly-typed identifier for this entity.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
	protected Entity(TId id)
	{
		Id = id ?? throw new ArgumentNullException(nameof(id));
	}

	/// <summary>
	/// Parameterless constructor reserved for EF Core materialisation.
	/// </summary>
	protected Entity()
	{
		Id = default!;
	}
	#endregion

	#region Audit
	/// <summary>
	/// Explicit interface implementation that allows the persistence pipeline to initialise
	/// audit metadata without exposing the setter publicly on the domain class.
	/// </summary>
	/// <param name="audit">The <see cref="AuditInfo"/> value object to assign.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="audit"/> is null.</exception>
	void IAuditableEntity.SetAudit(AuditInfo audit)
	{
		ArgumentNullException.ThrowIfNull(audit);
		Audit = audit;
	}

	/// <summary>
	/// Initialises the audit record on entity creation.
	/// Must be called once by the factory method before the entity is returned.
	/// </summary>
	/// <param name="createdOnUtc">UTC timestamp of creation.</param>
	/// <param name="createdBy">Actor who created this entity.</param>
	protected void InitializeAudit(DateTime createdOnUtc, string createdBy)
	{
		Audit = AuditInfo.Create(createdOnUtc, createdBy);
	}

	/// <summary>
	/// Updates the audit record to reflect a modification.
	/// Throws if audit has not been initialised.
	/// </summary>
	/// <param name="modifiedOnUtc">UTC timestamp of modification.</param>
	/// <param name="modifiedBy">Actor who performed the modification.</param>
	protected void Touch(DateTime modifiedOnUtc, string modifiedBy)
	{
		EnsureAuditInitialized();
		Audit.Modify(modifiedOnUtc, modifiedBy);
	}

	/// <summary>
	/// Protected overload of <see cref="IAuditableEntity.SetAudit"/> available to derived classes
	/// for explicit audit assignment (e.g., during reconstitution).
	/// </summary>
	/// <param name="audit">The <see cref="AuditInfo"/> value to assign.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="audit"/> is null.</exception>
	protected void SetAudit(AuditInfo audit)
	{
		ArgumentNullException.ThrowIfNull(audit);
		Audit = audit;
	}
	#endregion

	#region Soft Delete
	/// <summary>
	/// Explicit interface implementation — marks this entity as soft-deleted.
	/// Called by the repository or domain method via the interface contract.
	/// </summary>
	/// <param name="deletedOnUtc">UTC timestamp of deletion.</param>
	/// <param name="deletedBy">Actor who performed the deletion.</param>
	void ISoftDeletableEntity.MarkDeleted(DateTime deletedOnUtc, string deletedBy)
	{
		SoftDelete(deletedOnUtc, deletedBy);
	}

	/// <summary>
	/// Explicit interface implementation — restores a previously soft-deleted entity.
	/// Called by the repository or domain method via the interface contract.
	/// </summary>
	/// <param name="restoredOnUtc">UTC timestamp of restoration.</param>
	/// <param name="restoredBy">Actor who performed the restoration.</param>
	void ISoftDeletableEntity.MarkRestored(DateTime restoredOnUtc, string restoredBy)
	{
		Restore(restoredOnUtc, restoredBy);
	}

	/// <summary>
	/// Soft-deletes this entity. Idempotent — calling twice has no effect.
	/// Also updates audit metadata to reflect the deletion.
	/// </summary>
	/// <param name="deletedOnUtc">UTC timestamp of deletion.</param>
	/// <param name="deletedBy">Actor who performed the deletion.</param>
	protected void SoftDelete(DateTime deletedOnUtc, string deletedBy)
	{
		if (IsDeleted)
		{
			return;
		}

		ValidateUtcDate(deletedOnUtc, nameof(deletedOnUtc));
		ValidateActor(deletedBy, nameof(deletedBy));

		IsDeleted = true;
		DeletedOnUtc = deletedOnUtc;
		DeletedBy = deletedBy;

		if (Audit is not null)
		{
			Audit.Modify(deletedOnUtc, deletedBy);
		}
	}

	/// <summary>
	/// Restores a previously soft-deleted entity. Idempotent — calling twice has no effect.
	/// Also updates audit metadata to reflect the restoration.
	/// </summary>
	/// <param name="restoredOnUtc">UTC timestamp of restoration.</param>
	/// <param name="restoredBy">Actor who performed the restoration.</param>
	protected void Restore(DateTime restoredOnUtc, string restoredBy)
	{
		if (!IsDeleted)
		{
			return;
		}

		ValidateUtcDate(restoredOnUtc, nameof(restoredOnUtc));
		ValidateActor(restoredBy, nameof(restoredBy));

		IsDeleted = false;
		DeletedOnUtc = null;
		DeletedBy = null;

		if (Audit is not null)
		{
			Audit.Modify(restoredOnUtc, restoredBy);
		}
	}
	#endregion

	#region Equality
	/// <summary>
	/// Determines whether this entity is equal to another object.
	/// Equality is identity-based: two entities are equal if and only if they share the same runtime type
	/// and the same <see cref="Id"/>.
	/// </summary>
	/// <param name="obj">The object to compare against.</param>
	/// <returns><c>true</c> when both entities have the same type and the same identifier; otherwise <c>false</c>.</returns>
	public override bool Equals(object? obj)
	{
		if (obj is null)
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != GetType())
		{
			return false;
		}

		return obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);
	}

	/// <summary>
	/// Returns a hash code derived from the entity's runtime type and identifier.
	/// </summary>
	/// <returns>A hash code for this entity.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(GetType(), Id);
	}

	/// <summary>
	/// Equality operator. Returns <c>true</c> when both operands refer to the same entity.
	/// </summary>
	/// <param name="left">Left operand.</param>
	/// <param name="right">Right operand.</param>
	/// <returns><c>true</c> when equal; otherwise <c>false</c>.</returns>
	public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
	{
		return Equals(left, right);
	}

	/// <summary>
	/// Inequality operator. Returns <c>true</c> when the operands refer to different entities.
	/// </summary>
	/// <param name="left">Left operand.</param>
	/// <param name="right">Right operand.</param>
	/// <returns><c>true</c> when not equal; otherwise <c>false</c>.</returns>
	public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
	{
		return !Equals(left, right);
	}
	#endregion

	#region Private Helpers

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> when <see cref="Audit"/> has not been
	/// initialised. Guards against calling <see cref="Touch"/> before creation.
	/// </summary>
	private void EnsureAuditInitialized()
	{
		if (Audit is null)
		{
			throw new InvalidOperationException("AuditInfo must be initialised before performing audit updates. " + "Call InitializeAudit() during entity creation or ensure the persistence layer materialises Audit.");
		}
	}

	/// <summary>
	/// Validates that the actor string is non-null and non-whitespace.
	/// </summary>
	/// <param name="actor">The actor value to validate.</param>
	/// <param name="paramName">The parameter name for the exception message.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="actor"/> is null or whitespace.</exception>
	private static void ValidateActor(string actor, string paramName)
	{
		if (string.IsNullOrWhiteSpace(actor))
		{
			throw new ArgumentNullException(paramName);
		}
	}

	/// <summary>
	/// Validates that the date is a valid UTC <see cref="DateTime"/> (not <see cref="DateTime.MinValue"/>
	/// or <see cref="DateTime.MaxValue"/>, and has <see cref="DateTimeKind.Utc"/> kind).
	/// </summary>
	/// <param name="dateUtc">The date value to validate.</param>
	/// <param name="paramName">The parameter name for the exception message.</param>
	/// <exception cref="ArgumentException">Thrown when the date is invalid or not UTC.</exception>
	private static void ValidateUtcDate(DateTime dateUtc, string paramName)
	{
		if (dateUtc == DateTime.MaxValue || dateUtc == DateTime.MinValue)
		{
			throw new ArgumentException("Date must be a valid value.", paramName);
		}

		if (dateUtc.Kind != DateTimeKind.Utc)
		{
			throw new ArgumentException("Date must be expressed in UTC (DateTimeKind.Utc).", paramName);
		}
	}
	#endregion
}
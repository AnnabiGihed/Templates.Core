using System;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Marks an entity as auditable.
///              Auditable entities expose an <see cref="AuditInfo"/> value object containing
///              creation and last-modification metadata.
/// </summary>
public interface IAuditableEntity
{
	/// <summary>
	/// Gets the audit information associated with the entity.
	/// </summary>
	AuditInfo Audit { get; }

	/// <summary>
	/// Sets audit information (used by persistence pipeline for initialization/materialization).
	/// </summary>
	/// <param name="audit">The audit value object to assign.</param>
	void SetAudit(AuditInfo audit);
}

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Value object storing audit metadata for entities (Created/Modified by and timestamps).
///              Designed to be mapped as an owned type by the persistence layer.
/// </summary>
public class AuditInfo : ValueObject<AuditInfo>
{
	#region Properties
	public DateTime CreatedOnUtc { get; protected set; }
	public string? CreatedBy { get; protected set; } = default!;
	public string? ModifiedBy { get; protected set; } = default!;
	public DateTime? ModifiedOnUtc { get; protected set; }
	#endregion

	#region Constructors
	private AuditInfo() { }

	[JsonConstructor]
	public AuditInfo(string createdBy, string modifiedBy, DateTime createdOnUtc, DateTime? modifiedOnUtc)
	{
		ValidateUtcDate(createdOnUtc, nameof(createdOnUtc));
		if (modifiedOnUtc.HasValue)
			ValidateUtcDate(modifiedOnUtc.Value, nameof(modifiedOnUtc));

		ValidateAuthor(createdBy, nameof(createdBy));
		ValidateAuthor(modifiedBy, nameof(modifiedBy));

		CreatedBy = createdBy;
		ModifiedBy = modifiedBy;
		CreatedOnUtc = createdOnUtc;
		ModifiedOnUtc = modifiedOnUtc;
	}
	#endregion

	public static AuditInfo Create(DateTime dateUtc, string author)
	{
		ValidateUtcDate(dateUtc, nameof(dateUtc));
		ValidateAuthor(author, nameof(author));

		return new AuditInfo(author, author, dateUtc, dateUtc);
	}

	public void Modify(DateTime dateUtc, string author)
	{
		ValidateUtcDate(dateUtc, nameof(dateUtc));
		ValidateAuthor(author, nameof(author));

		ModifiedBy = author;
		ModifiedOnUtc = dateUtc;
	}

	public void Update(string createdBy, string modifiedBy, DateTime createdOnUtc, DateTime modifiedOnUtc)
	{
		ValidateUtcDate(createdOnUtc, nameof(createdOnUtc));
		ValidateUtcDate(modifiedOnUtc, nameof(modifiedOnUtc));

		ValidateAuthor(createdBy, nameof(createdBy));
		ValidateAuthor(modifiedBy, nameof(modifiedBy));

		CreatedBy = createdBy;
		ModifiedBy = modifiedBy;
		CreatedOnUtc = createdOnUtc;
		ModifiedOnUtc = modifiedOnUtc;
	}

	protected override bool EqualsCore(AuditInfo other)
	{
		return CreatedOnUtc == other.CreatedOnUtc
			   && string.Equals(CreatedBy, other.CreatedBy, StringComparison.Ordinal)
			   && string.Equals(ModifiedBy, other.ModifiedBy, StringComparison.Ordinal)
			   && Nullable.Equals(ModifiedOnUtc, other.ModifiedOnUtc);
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(
			CreatedOnUtc,
			CreatedBy is null ? 0 : StringComparer.Ordinal.GetHashCode(CreatedBy),
			ModifiedBy is null ? 0 : StringComparer.Ordinal.GetHashCode(ModifiedBy),
			ModifiedOnUtc);
	}

	private static void ValidateAuthor(string author, string paramName)
	{
		if (string.IsNullOrWhiteSpace(author))
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

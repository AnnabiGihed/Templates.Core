using CSharpFunctionalExtensions;
using System.Text.Json.Serialization;

namespace Templates.Core.Domain.Primitives;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Value object that stores creation and last-modification metadata for an entity.
///              Tracks who created or last modified a record and when.
///              Mapped as an EF Core owned type — persisted inline with the owning entity row.
///              Mutation is allowed only through the controlled <see cref="Modify"/> and
///              <see cref="Update"/> methods; no public setters are exposed.
/// </summary>
public sealed class AuditInfo : ValueObject<AuditInfo>
{
	#region Properties
	/// <summary>
	/// Gets the identifier of the actor who created the owning entity.
	/// </summary>
	public string? CreatedBy { get; private set; }

	/// <summary>
	/// Gets the identifier of the actor who last modified the owning entity.
	/// </summary>
	public string? ModifiedBy { get; private set; }

	/// <summary>
	/// Gets the UTC timestamp at which the owning entity was created.
	/// </summary>
	public DateTime CreatedOnUtc { get; private set; }

	/// <summary>
	/// Gets the UTC timestamp of the most recent modification to the owning entity.
	/// <c>null</c> when the entity has never been modified after creation.
	/// </summary>
	public DateTime? ModifiedOnUtc { get; private set; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor reserved for EF Core materialisation.
	/// </summary>
	private AuditInfo()
	{
	}

	/// <summary>
	/// Full constructor used by <see cref="System.Text.Json"/> deserialisation.
	/// Validates all parameters before assigning properties.
	/// </summary>
	/// <param name="createdBy">Actor who created the entity.</param>
	/// <param name="modifiedBy">Actor who last modified the entity.</param>
	/// <param name="createdOnUtc">UTC creation timestamp.</param>
	/// <param name="modifiedOnUtc">UTC last-modification timestamp, or <c>null</c> if never modified.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="createdBy"/> or <paramref name="modifiedBy"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="createdOnUtc"/> is not a valid UTC date.</exception>
	[JsonConstructor]
	private AuditInfo(string createdBy, string modifiedBy, DateTime createdOnUtc, DateTime? modifiedOnUtc)
	{
		ValidateUtcDate(createdOnUtc, nameof(createdOnUtc));

		if (modifiedOnUtc.HasValue)
		{
			ValidateUtcDate(modifiedOnUtc.Value, nameof(modifiedOnUtc));
		}

		ValidateAuthor(createdBy, nameof(createdBy));
		ValidateAuthor(modifiedBy, nameof(modifiedBy));

		CreatedBy = createdBy;
		ModifiedBy = modifiedBy;
		CreatedOnUtc = createdOnUtc;
		ModifiedOnUtc = modifiedOnUtc;
	}

	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="AuditInfo"/> instance representing the initial creation of an entity.
	/// Both <c>CreatedBy</c> and <c>ModifiedBy</c> are set to <paramref name="author"/>.
	/// </summary>
	/// <param name="dateUtc">UTC timestamp of creation. Must be a valid UTC <see cref="DateTime"/>.</param>
	/// <param name="author">The actor performing the creation. Must be non-null and non-whitespace.</param>
	/// <returns>A fully initialised <see cref="AuditInfo"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="author"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="dateUtc"/> is not a valid UTC date.</exception>
	public static AuditInfo Create(DateTime dateUtc, string author)
	{
		ValidateUtcDate(dateUtc, nameof(dateUtc));
		ValidateAuthor(author, nameof(author));

		return new AuditInfo(author, author, dateUtc, dateUtc);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates <see cref="ModifiedBy"/> and <see cref="ModifiedOnUtc"/> to reflect a modification.
	/// </summary>
	/// <param name="dateUtc">UTC timestamp of the modification.</param>
	/// <param name="author">Actor who performed the modification.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="author"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="dateUtc"/> is not a valid UTC date.</exception>
	public void Modify(DateTime dateUtc, string author)
	{
		ValidateUtcDate(dateUtc, nameof(dateUtc));
		ValidateAuthor(author, nameof(author));

		ModifiedBy = author;
		ModifiedOnUtc = dateUtc;
	}

	/// <summary>
	/// Replaces all four audit fields at once.
	/// Intended for migrations or administrative corrections — use <see cref="Modify"/> for routine updates.
	/// </summary>
	/// <param name="createdBy">New creation actor.</param>
	/// <param name="modifiedBy">New modification actor.</param>
	/// <param name="createdOnUtc">New creation timestamp (UTC).</param>
	/// <param name="modifiedOnUtc">New last-modification timestamp (UTC).</param>
	/// <exception cref="ArgumentNullException">Thrown when any actor string is null or whitespace.</exception>
	/// <exception cref="ArgumentException">Thrown when any timestamp is not a valid UTC date.</exception>
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
	#endregion

	#region Equality
	/// <summary>
	/// Determines structural equality between two <see cref="AuditInfo"/> instances.
	/// All four fields must match for two instances to be considered equal.
	/// </summary>
	/// <param name="other">The other <see cref="AuditInfo"/> to compare against.</param>
	/// <returns><c>true</c> when all fields match; otherwise <c>false</c>.</returns>
	protected override bool EqualsCore(AuditInfo other)
	{
		return CreatedOnUtc == other.CreatedOnUtc
			&& string.Equals(CreatedBy, other.CreatedBy, StringComparison.Ordinal)
			&& string.Equals(ModifiedBy, other.ModifiedBy, StringComparison.Ordinal)
			&& Nullable.Equals(ModifiedOnUtc, other.ModifiedOnUtc);
	}

	/// <summary>
	/// Returns a hash code derived from all four audit fields.
	/// </summary>
	/// <returns>A hash code for this <see cref="AuditInfo"/> instance.</returns>
	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(
			CreatedOnUtc,
			CreatedBy is null ? 0 : StringComparer.Ordinal.GetHashCode(CreatedBy),
			ModifiedBy is null ? 0 : StringComparer.Ordinal.GetHashCode(ModifiedBy),
			ModifiedOnUtc);
	}
	#endregion

	#region Private Helpers
	/// <summary>
	/// Guards that the actor string is non-null and non-whitespace.
	/// </summary>
	/// <param name="author">The actor value to validate.</param>
	/// <param name="paramName">Parameter name used in the exception message.</param>
	/// <exception cref="ArgumentNullException">Thrown when the actor is null or whitespace.</exception>
	private static void ValidateAuthor(string author, string paramName)
	{
		if (string.IsNullOrWhiteSpace(author))
			throw new ArgumentNullException(paramName);
	}

	/// <summary>
	/// Guards that the date is a valid UTC <see cref="DateTime"/>.
	/// Rejects <see cref="DateTime.MinValue"/>, <see cref="DateTime.MaxValue"/>, and non-UTC kinds.
	/// </summary>
	/// <param name="dateUtc">The date value to validate.</param>
	/// <param name="paramName">Parameter name used in the exception message.</param>
	/// <exception cref="ArgumentException">Thrown when the date is out of range or not UTC.</exception>
	private static void ValidateUtcDate(DateTime dateUtc, string paramName)
	{
		if (dateUtc == DateTime.MaxValue || dateUtc == DateTime.MinValue)
			throw new ArgumentException("Date must be a valid value.", paramName);

		if (dateUtc.Kind != DateTimeKind.Utc)
			throw new ArgumentException("Date must be expressed in UTC (DateTimeKind.Utc).", paramName);
	}
	#endregion
}
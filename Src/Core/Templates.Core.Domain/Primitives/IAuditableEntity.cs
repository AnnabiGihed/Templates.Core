using CSharpFunctionalExtensions;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Templates.Core.Domain.Primitives;

public interface IAuditableEntity
{
	AuditInfo Audit { get; }
}


[ComplexType]
public class AuditInfo : ValueObject<AuditInfo>
{
	#region Properties
	public DateTime CreatedOnUtc { get; protected set; }
	public string? CreatedBy { get; protected set; } = default!;
	public string? ModifiedBy { get; protected set; } = default!;
	public DateTime? ModifiedOnUtc { get; protected set; } = default!;
	#endregion

	#region Constructors
	private AuditInfo()
	{
		
	}

	[JsonConstructor]
	public AuditInfo(string createdBy, string modifiedBy, DateTime createdOnUtc, DateTime modifiedOnUtc)
	{
		CreatedBy = createdBy;
		ModifiedBy = modifiedBy;
		CreatedOnUtc = createdOnUtc;
		ModifiedOnUtc = modifiedOnUtc;
	}
	#endregion

	public static AuditInfo Create(DateTime date, string author)
	{

		if (date == DateTime.MaxValue || date == DateTime.MinValue)
			throw new ArgumentException(nameof(date));

		if (string.IsNullOrEmpty(author))
			throw new ArgumentNullException(nameof(author));

		return new AuditInfo(author, author, date, date);
	}

	public void Modify(DateTime date, string author)
	{

		if (date == DateTime.MaxValue || date == DateTime.MinValue)
			throw new ArgumentException(nameof(date));

		if (string.IsNullOrEmpty(author))
			throw new ArgumentNullException(nameof(author));

		ModifiedBy = author;
		ModifiedOnUtc = date;
	}

	public void Update(string createdBy, string modifiedBy, DateTime createdOnUtc, DateTime modifiedOnUtc)
	{
		if (createdOnUtc == DateTime.MaxValue || createdOnUtc == DateTime.MinValue)
			throw new ArgumentException(nameof(createdOnUtc));

		if (modifiedOnUtc == DateTime.MaxValue || modifiedOnUtc == DateTime.MinValue)
			throw new ArgumentException(nameof(modifiedOnUtc));

		if (string.IsNullOrEmpty(createdBy))
			throw new ArgumentNullException(nameof(createdBy));

		if (string.IsNullOrEmpty(modifiedBy))
			throw new ArgumentNullException(nameof(modifiedBy));

		CreatedBy = createdBy;
		ModifiedBy = modifiedBy;
		CreatedOnUtc = createdOnUtc;
		ModifiedOnUtc = modifiedOnUtc;
	}

	protected override bool EqualsCore(AuditInfo other)
	{
		return this == other;
	}

	protected override int GetHashCodeCore()
	{
		return this.GetHashCodeCore();
	}
}
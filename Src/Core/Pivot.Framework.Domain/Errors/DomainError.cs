using CSharpFunctionalExtensions;
using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Domain.Errors;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Value object representing a domain-level error descriptor.
///              A DomainError carries a stable machine-readable <see cref="Code"/> and a
///              human-readable (typically localised) <see cref="Message"/>.
///              It supports compact serialisation and deserialisation for transport, persistence, and logging
///              using a <c>||</c> separator. Equality is based solely on <see cref="Code"/>
///              to allow de-duplication across different message phrasings.
/// </summary>
public sealed partial class DomainError : ValueObject<DomainError>
{
	#region Fields
	/// <summary>
	/// The string separator used when serialising a <see cref="DomainError"/> to a single string.
	/// </summary>
	private const string Separator = "||";
	#endregion

	#region Properties
	/// <summary>
	/// Gets the stable, machine-readable domain error code.
	/// Used for identification, localisation key mapping, and de-duplication.
	/// Follows the <c>ClassName.PropertyName.Reason</c> convention (e.g., <c>Order.Status.Invalid</c>).
	/// </summary>
	public string Code { get; }

	/// <summary>
	/// Gets the human-readable (typically localised) error message associated with this error.
	/// </summary>
	public string Message { get; }
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new <see cref="DomainError"/> with the given code and message.
	/// </summary>
	/// <param name="code">The stable domain error code. Must be non-null and non-whitespace.</param>
	/// <param name="message">The localised error message. Must be non-null.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
	public DomainError(string code, string message)
	{
		if (string.IsNullOrWhiteSpace(code))
			throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code));

		ArgumentNullException.ThrowIfNull(message);

		Code = code;
		Message = message;
	}
	#endregion

	#region Serialisation
	/// <summary>
	/// Serialises this <see cref="DomainError"/> to the compact transport format: <c>{Code}||{Message}</c>.
	/// </summary>
	/// <returns>The serialised string representation of this error.</returns>
	public string Serialize()
	{
		return $"{Code}{Separator}{Message}";
	}

	/// <summary>
	/// Deserialises a compact error string (produced by <see cref="Serialize"/>) back into
	/// a <see cref="CSharpFunctionalExtensions.Error"/> instance.
	/// </summary>
	/// <param name="serialized">The serialised error string to parse.</param>
	/// <returns>An <see cref="CSharpFunctionalExtensions.Error"/> instance with the original code and message.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="serialized"/> is null or whitespace.</exception>
	/// <exception cref="FormatException">Thrown when the string does not conform to the expected <c>Code||Message</c> format.</exception>
	public static Error Deserialize(string serialized)
	{
		if (string.IsNullOrWhiteSpace(serialized))
		{
			throw new ArgumentException("Serialized error cannot be null or whitespace.", nameof(serialized));
		}

		// Backward-compat / special-case mapping retained for existing persisted error strings.
		if (serialized == $"{Resource.ANoneEmptyRequestBodyIsRequired}")
		{
			return BaseDomainErrors.General.ValueIsRequired(null);
		}

		// Split into exactly 2 parts: Code and Message (the message itself may contain the separator).
		var parts = serialized.Split(new[] { Separator }, count: 2, StringSplitOptions.None);

		if (parts.Length != 2)
		{
			throw new FormatException($"{Resource.InvalidErrorSerialization}:'{serialized}'");
		}

		return new Error(parts[0], parts[1]);
	}
	#endregion

	#region Label Helpers
	/// <summary>
	/// Builds a localised label that references a field name — e.g., <c>" for field X "</c>.
	/// Used when constructing error messages that need to identify which field is affected.
	/// When <paramref name="name"/> is <c>null</c>, returns a single whitespace separator.
	/// </summary>
	/// <param name="name">The field name to include in the label, or <c>null</c> for a blank label.</param>
	/// <returns>A formatted label string ready for use in an error message.</returns>
	public static string ForField(string? name = null)
	{
		if (name is null)
			return " ";

		return " " + string.Format(Resource.ForField, name) + " ";
	}

	/// <summary>
	/// Builds a localised label that references both a field name and an invalid value —
	/// e.g., <c>" value 'X' for field Y "</c>.
	/// Falls back to field-only or value-only labels when one argument is absent.
	/// </summary>
	/// <param name="name">The field name, or <c>null</c> to omit it from the label.</param>
	/// <param name="value">The field value, or <c>null</c> to omit it from the label.</param>
	/// <returns>A formatted label string ready for use in an error message.</returns>
	public static string ValueForField(string? name = null, string? value = null)
	{
		var label = " ";

		if (name is not null && value is not null)
		{
			label += string.Format(Resource.ValueForField, value, name) + " ";
		}
		else if (name is not null)
		{
			label += name + " ";
		}
		else if (value is not null)
		{
			label += value + " ";
		}

		return label;
	}
	#endregion

	#region Equality
	/// <summary>
	/// Determines structural equality between two <see cref="DomainError"/> instances.
	/// Equality is based solely on <see cref="Code"/> so that two errors for the same
	/// domain concept compare as equal regardless of localisation differences in their messages.
	/// </summary>
	/// <param name="other">The other <see cref="DomainError"/> to compare against.</param>
	/// <returns><c>true</c> when both errors share the same <see cref="Code"/>; otherwise <c>false</c>.</returns>
	protected override bool EqualsCore(DomainError other)
	{
		return Code == other.Code;
	}

	/// <summary>
	/// Returns a hash code derived from <see cref="Code"/> using ordinal string comparison.
	/// </summary>
	/// <returns>A hash code for this <see cref="DomainError"/>.</returns>
	protected override int GetHashCodeCore()
	{
		return Code.GetHashCode(StringComparison.Ordinal);
	}
	#endregion

	#region Internal Helpers
	/// <summary>
	/// Validates that both descriptor arguments are non-null and non-whitespace.
	/// Used by overloads that accept explicit code and message descriptors.
	/// </summary>
	/// <param name="codeDescriptor">The error code descriptor string to validate.</param>
	/// <param name="propertyDescriptor">The message template descriptor string to validate.</param>
	/// <exception cref="ArgumentException">Thrown when either descriptor is null or whitespace.</exception>
	internal static void ValidateDescriptors(string codeDescriptor, string propertyDescriptor)
	{
		if (string.IsNullOrWhiteSpace(codeDescriptor))
			throw new ArgumentException("Code descriptor cannot be null or whitespace.", nameof(codeDescriptor));

		if (string.IsNullOrWhiteSpace(propertyDescriptor))
			throw new ArgumentException("Property descriptor cannot be null or whitespace.", nameof(propertyDescriptor));
	}
	#endregion
}
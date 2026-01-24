using CSharpFunctionalExtensions;
using Templates.Core.Domain.Shared;

namespace Templates.Core.Domain.Errors;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Value object representing a domain-level error descriptor.
///              A DomainError is identified by a stable Code and provides a human-readable Message.
///              It also supports compact serialization/deserialization for transport, persistence, or logging.
/// </summary>
/// <remarks>
/// Equality is intentionally based on <see cref="Code"/> only to allow de-duplication by error code.
/// </remarks>
public sealed partial class DomainError : ValueObject<DomainError>
{
	private const string Separator = "||";

	/// <summary>
	/// Gets the stable domain error code (used for identification and localization mapping).
	/// </summary>
	public string Code { get; }

	/// <summary>
	/// Gets the human-readable (typically localized) error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Initializes a new instance of <see cref="DomainError"/>.
	/// </summary>
	/// <param name="code">The stable domain error code.</param>
	/// <param name="message">The localized error message.</param>
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

	/// <summary>
	/// Serializes the current <see cref="DomainError"/> into a compact string format: <c>{Code}||{Message}</c>.
	/// </summary>
	/// <returns>The serialized representation.</returns>
	public string Serialize() => $"{Code}{Separator}{Message}";

	/// <summary>
	/// Deserializes a compact string into a <see cref="Error"/> (CSharpFunctionalExtensions).
	/// </summary>
	/// <param name="serialized">The serialized error string.</param>
	/// <returns>An <see cref="Error"/> instance.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="serialized"/> is null or whitespace.</exception>
	/// <exception cref="FormatException">Thrown when the string does not match the expected format.</exception>
	public static Error Deserialize(string serialized)
	{
		if (string.IsNullOrWhiteSpace(serialized))
			throw new ArgumentException("Serialized error cannot be null or whitespace.", nameof(serialized));

		// Backward-compat / special-case mapping retained as-is.
		if (serialized == $"{Resource.ANoneEmptyRequestBodyIsRequired}")
			return BaseDomainErrors.General.ValueIsRequired(null);

		// Split into exactly 2 parts: Code and Message (message may contain separators afterwards).
		var parts = serialized.Split(new[] { Separator }, count: 2, StringSplitOptions.None);

		if (parts.Length != 2)
			throw new FormatException($"{Resource.InvalidErrorSerialization}:'{serialized}'");

		return new Error(parts[0], parts[1]);
	}

	/// <summary>
	/// Determines equality for <see cref="DomainError"/> instances (Code-based).
	/// </summary>
	protected override bool EqualsCore(DomainError other) => Code == other.Code;

	/// <summary>
	/// Returns a hash code based on <see cref="Code"/> (ordinal).
	/// </summary>
	protected override int GetHashCodeCore() => Code.GetHashCode(StringComparison.Ordinal);

	/// <summary>
	/// Builds a label referencing a field name using resources (e.g., " for field X ").
	/// </summary>
	/// <param name="name">The field name.</param>
	/// <returns>A formatted label string.</returns>
	public static string ForField(string? name = null)
	{
		return name is null
			? " "
			: " " + string.Format(Resource.ForField, name) + " ";
	}

	/// <summary>
	/// Builds a label referencing a field name and/or field value using resources.
	/// </summary>
	/// <param name="name">The field name.</param>
	/// <param name="value">The field value.</param>
	/// <returns>A formatted label string.</returns>
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

	/// <summary>
	/// Validates custom descriptor inputs for overloads that accept explicit resource descriptors.
	/// </summary>
	/// <param name="codeDescriptor">The code descriptor.</param>
	/// <param name="propertyDescriptor">The message/template descriptor.</param>
	/// <exception cref="ArgumentException">Thrown when one of the descriptors is null or whitespace.</exception>
	internal static void ValidateDescriptors(string codeDescriptor, string propertyDescriptor)
	{
		if (string.IsNullOrWhiteSpace(codeDescriptor))
			throw new ArgumentException("Code descriptor cannot be null or whitespace.", nameof(codeDescriptor));

		if (string.IsNullOrWhiteSpace(propertyDescriptor))
			throw new ArgumentException("Property descriptor cannot be null or whitespace.", nameof(propertyDescriptor));
	}
}

using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Exceptions;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Application-level exception representing a missing resource (HTTP 404).
///              Typically thrown at API/application boundaries when a requested entity
///              cannot be found.
/// </summary>
public sealed class NotFoundException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NotFoundException"/> class.
	/// </summary>
	/// <param name="name">The name of the resource/entity type.</param>
	/// <param name="key">The key/identifier value that was not found.</param>
	public NotFoundException(string name, object key)
		: base(BuildMessage(name, key))
	{
		ResourceName = name;
		ResourceKey = key;
	}

	/// <summary>
	/// Gets the resource/entity name.
	/// </summary>
	public string ResourceName { get; }

	/// <summary>
	/// Gets the key/identifier value that was not found.
	/// </summary>
	public object ResourceKey { get; }

	private static string BuildMessage(string name, object key)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		ArgumentNullException.ThrowIfNull(key);

		return $"{name} ({key}) was not found.";
	}

	/// <summary>
	/// Convenience factory to create a standardized <see cref="Error"/> for not found results.
	/// Useful when returning <see cref="Result"/> instead of throwing.
	/// </summary>
	public static Error ToError(string name, object key)
		=> new("Error.NotFound", $"{BuildMessage(name, key)}");
}

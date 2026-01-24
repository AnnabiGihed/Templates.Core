using Templates.Core.Domain.Shared;

namespace Templates.Core.Domain.Errors;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Centralized catalog of reusable domain errors.
///              Exposes factories returning <see cref="Error"/> to integrate with CSharpFunctionalExtensions.
///              Provides two overload shapes:
///              - Default overloads (use standard resource keys)
///              - Descriptor overloads (require explicit code and message template descriptors)
///              Descriptor overloads intentionally do NOT use optional parameters to avoid ambiguous overload resolution.
/// </summary>
public static class BaseDomainErrors
{
	/// <summary>
	/// General-purpose domain errors (CRUD, validation, length constraints, collections, etc.).
	/// </summary>
	public static class General
	{
		#region NotFound

		private static DomainError NotFoundDomainError(Guid? id = null)
		{
			var forId = id is null ? "" : " " + string.Format(Resource.ForId, id);
			return new(Resource.record_not_found, string.Format(Resource.RecordNotFound, forId));
		}

		/// <summary>
		/// Creates a not-found error optionally scoped to an entity identifier.
		/// </summary>
		public static Error NotFound(Guid? id = null)
		{
			var domainError = NotFoundDomainError(id);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError NotFoundDomainError(Guid? id, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);

			var forId = id is null ? "" : " " + string.Format(Resource.ForId, id);
			return new(codeDescriptor, string.Format(propertyDescriptor, forId));
		}

		/// <summary>
		/// Creates a not-found error using explicit code and message template descriptors.
		/// </summary>
		public static Error NotFound(Guid? id, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = NotFoundDomainError(id, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion NotFound

		#region ValueIsInvalid

		private static DomainError ValueIsInvalidDomainError(string? name = null, string? value = null)
		{
			return new(Resource.value_is_invalid, string.Format(Resource.ValueIsInvalid, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates an error indicating a value is invalid, optionally scoped to a field name and value.
		/// </summary>
		public static Error ValueIsInvalid(string? name = null, string? value = null)
		{
			var domainError = ValueIsInvalidDomainError(name, value);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueIsInvalidDomainError(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates an invalid-value error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueIsInvalid(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueIsInvalidDomainError(name, value, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueIsInvalid

		#region ValueAlreadyExists

		private static DomainError ValueAlreadyExistsDomainError(string? name = null, string? value = null)
		{
			return new(Resource.value_already_exists, string.Format(Resource.ValueAlreadyExists, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates an error indicating a value already exists, optionally scoped to a field name and value.
		/// </summary>
		public static Error ValueAlreadyExists(string? name = null, string? value = null)
		{
			var domainError = ValueAlreadyExistsDomainError(name, value);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueAlreadyExistsDomainError(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates an already-exists error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueAlreadyExists(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueAlreadyExistsDomainError(name, value, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueAlreadyExists

		#region ValueIsToLong

		private static DomainError ValueIsToLongDomainError(string? name = null, string? value = null, int maxLength = 0)
		{
			return new(Resource.value_is_tolong, string.Format(Resource.ValueIsToLong, DomainError.ValueForField(name, value), maxLength));
		}

		/// <summary>
		/// Creates an error indicating a value is too long.
		/// </summary>
		public static Error ValueIsToLong(string? name = null, string? value = null, int maxLength = 0)
		{
			var domainError = ValueIsToLongDomainError(name, value, maxLength);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueIsToLongDomainError(string? name, string? value, int maxLength, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ValueForField(name, value), maxLength));
		}

		/// <summary>
		/// Creates a too-long error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueIsToLong(string? name, string? value, int maxLength, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueIsToLongDomainError(name, value, maxLength, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueIsToLong

		#region ValueIsToShort

		private static DomainError ValueIsToShortDomainError(string? name = null, string? value = null, int minLength = 0)
		{
			return new(Resource.value_is_toshort, string.Format(Resource.ValueIsToShort, DomainError.ValueForField(name, value), minLength));
		}

		/// <summary>
		/// Creates an error indicating a value is too short.
		/// </summary>
		public static Error ValueIsToShort(string? name = null, string? value = null, int minLength = 0)
		{
			var domainError = ValueIsToShortDomainError(name, value, minLength);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueIsToShortDomainError(string? name, string? value, int minLength, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ValueForField(name, value), minLength));
		}

		/// <summary>
		/// Creates a too-short error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueIsToShort(string? name, string? value, int minLength, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueIsToShortDomainError(name, value, minLength, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueIsToShort

		#region ValueIsRequired

		private static DomainError ValueIsRequiredDomainError(string? name = null)
		{
			return new(Resource.value_is_required, string.Format(Resource.Required, DomainError.ForField(name)));
		}

		/// <summary>
		/// Creates an error indicating a value is required, optionally scoped to a field name.
		/// </summary>
		public static Error ValueIsRequired(string? name = null)
		{
			var domainError = ValueIsRequiredDomainError(name);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueIsRequiredDomainError(string? name, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ForField(name)));
		}

		/// <summary>
		/// Creates a required-value error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueIsRequired(string? name, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueIsRequiredDomainError(name, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueIsRequired

		#region ValueNotNegative

		private static DomainError ValueNotNegativeDomainError(string? name = null, string? value = null)
		{
			return new(Resource.value_not_negative, string.Format(Resource.ValueIsNegative, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates an error indicating a value must not be negative.
		/// </summary>
		public static Error ValueNotNegative(string? name = null, string? value = null)
		{
			var domainError = ValueNotNegativeDomainError(name, value);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError ValueNotNegativeDomainError(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ValueForField(name, value)));
		}

		/// <summary>
		/// Creates a not-negative error using explicit code and message template descriptors.
		/// </summary>
		public static Error ValueNotNegative(string? name, string? value, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = ValueNotNegativeDomainError(name, value, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion ValueNotNegative

		#region InvalidLength

		private static DomainError InvalidLengthDomainError(string? name = null)
		{
			return new(Resource.invalid_string_length, string.Format(Resource.InvalidLength, DomainError.ForField(name)));
		}

		/// <summary>
		/// Creates an error indicating an invalid string length.
		/// </summary>
		public static Error InvalidLength(string? name = null)
		{
			var domainError = InvalidLengthDomainError(name);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError InvalidLengthDomainError(string? name, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, DomainError.ForField(name)));
		}

		/// <summary>
		/// Creates an invalid-length error using explicit code and message template descriptors.
		/// </summary>
		public static Error InvalidLength(string? name, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = InvalidLengthDomainError(name, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion InvalidLength

		#region CollectionIsTooSmall

		private static DomainError CollectionIsTooSmallDomainError(int min, int current)
		{
			return new(Resource.collection_is_too_small, string.Format(Resource.CollectionIsToSmall, min, current));
		}

		/// <summary>
		/// Creates an error indicating a collection has too few items.
		/// </summary>
		public static Error CollectionIsTooSmall(int min, int current)
		{
			var domainError = CollectionIsTooSmallDomainError(min, current);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError CollectionIsTooSmallDomainError(int min, int current, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, min, current));
		}

		/// <summary>
		/// Creates a too-small collection error using explicit code and message template descriptors.
		/// </summary>
		public static Error CollectionIsTooSmall(int min, int current, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = CollectionIsTooSmallDomainError(min, current, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion CollectionIsTooSmall

		#region CollectionIsTooLarge

		private static DomainError CollectionIsTooLargeDomainError(int max, int current)
		{
			return new(Resource.collection_is_too_large, string.Format(Resource.CollectionIsTooLarge, max, current));
		}

		/// <summary>
		/// Creates an error indicating a collection has too many items.
		/// </summary>
		public static Error CollectionIsTooLarge(int max, int current)
		{
			var domainError = CollectionIsTooLargeDomainError(max, current);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError CollectionIsTooLargeDomainError(int max, int current, string codeDescriptor, string propertyDescriptor)
		{
			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, max, current));
		}

		/// <summary>
		/// Creates a too-large collection error using explicit code and message template descriptors.
		/// </summary>
		public static Error CollectionIsTooLarge(int max, int current, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = CollectionIsTooLargeDomainError(max, current, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion CollectionIsTooLarge

		#region InternalServerError

		private static DomainError InternalServerErrorDomainError(string message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			return new(Resource.internal_server_error, message);
		}

		/// <summary>
		/// Creates an internal server error with a provided message.
		/// </summary>
		public static Error InternalServerError(string message)
		{
			var domainError = InternalServerErrorDomainError(message);
			return new Error(domainError.Code, domainError.Message);
		}

		private static DomainError InternalServerErrorDomainError(string message, string codeDescriptor, string propertyDescriptor)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
			return new(codeDescriptor, string.Format(propertyDescriptor, message));
		}

		/// <summary>
		/// Creates an internal server error using explicit code and message template descriptors.
		/// </summary>
		public static Error InternalServerError(string message, string codeDescriptor, string propertyDescriptor)
		{
			var domainError = InternalServerErrorDomainError(message, codeDescriptor, propertyDescriptor);
			return new Error(domainError.Code, domainError.Message);
		}

		#endregion InternalServerError
	}

	/// <summary>
	/// Domain errors related to value objects.
	/// </summary>
	public static class ValueObjects
	{
		/// <summary>
		/// Errors related to the Money value object.
		/// </summary>
		public static class Money
		{
			#region CurrencyMismatch

			private static DomainError CurrencyMismatchDomainError(string? name, string? value1, string? value2)
			{
				return new(Resource.value_is_invalid,
					string.Format(Resource.CurrencyMismatch,
						DomainError.ValueForField(name, $"{value1},{value2}")));
			}

			/// <summary>
			/// Creates an error indicating currencies do not match.
			/// </summary>
			public static Error CurrencyMismatch(string? name, string? value1, string? value2)
			{
				var domainError = CurrencyMismatchDomainError(name, value1, value2);
				return new Error(domainError.Code, domainError.Message);
			}

			private static DomainError CurrencyMismatchDomainError(string? name, string? value1, string? value2, string codeDescriptor, string propertyDescriptor)
			{
				DomainError.ValidateDescriptors(codeDescriptor, propertyDescriptor);
				return new(codeDescriptor,
					string.Format(propertyDescriptor,
						DomainError.ValueForField(name, $"{value1},{value2}")));
			}

			/// <summary>
			/// Creates a currency-mismatch error using explicit code and message template descriptors.
			/// </summary>
			public static Error CurrencyMismatch(string? name, string? value1, string? value2, string codeDescriptor, string propertyDescriptor)
			{
				var domainError = CurrencyMismatchDomainError(name, value1, value2, codeDescriptor, propertyDescriptor);
				return new Error(domainError.Code, domainError.Message);
			}

			#endregion CurrencyMismatch
		}
	}
}











using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Templates.Core.Domain.Primitives;

namespace Templates.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Applies EF Core conventions for domain primitives.
///              - Registers AuditInfo as an owned type
///              - Enforces consistent column naming for the Audit owned navigation
/// </summary>
public static class DomainPrimitivesModelBuilderExtensions
{
	private const string AuditNavigationName = nameof(IAuditableEntity.Audit);

	/// <summary>
	/// Applies EF Core mappings for domain primitives (e.g., owned value objects) to the model.
	/// </summary>
	/// <param name="modelBuilder">The EF Core model builder.</param>
	/// <returns>The same <see cref="ModelBuilder"/> instance for fluent chaining.</returns>
	public static ModelBuilder ApplyDomainPrimitives(this ModelBuilder modelBuilder)
	{
		// Declare the type as owned globally.
		// This allows EF to treat AuditInfo as an owned type wherever it is used.
		modelBuilder.Owned<AuditInfo>();

		// Enforce consistent column names for all entities implementing IAuditableEntity.
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			var clrType = entityType.ClrType;
			if (clrType is null)
				continue;

			if (!typeof(IAuditableEntity).IsAssignableFrom(clrType))
				continue;

			// Configure the owned navigation named "Audit" using non-generic API.
			modelBuilder.Entity(clrType).OwnsOne(
				ownedType: typeof(AuditInfo),
				navigationName: AuditNavigationName,
				buildAction: owned =>
				{
					owned.Property(nameof(AuditInfo.CreatedOnUtc)).HasColumnName("Audit_CreatedOnUtc");
					owned.Property(nameof(AuditInfo.CreatedBy)).HasColumnName("Audit_CreatedBy");
					owned.Property(nameof(AuditInfo.ModifiedOnUtc)).HasColumnName("Audit_ModifiedOnUtc");
					owned.Property(nameof(AuditInfo.ModifiedBy)).HasColumnName("Audit_ModifiedBy");

					// Optional but recommended: ensure the owned type is stored in the same table
					// and doesn't create its own table in edge cases.
					owned.ToTable(entityType.GetTableName()!);
				});
		}

		return modelBuilder;
	}
}

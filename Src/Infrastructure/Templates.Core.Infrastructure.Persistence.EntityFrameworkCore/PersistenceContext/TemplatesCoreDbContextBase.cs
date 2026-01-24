using Microsoft.EntityFrameworkCore;
using Templates.Persistence.EntityFrameworkCore.Configuration;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.PersistenceContext;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Base DbContext that applies shared EF Core conventions and mappings
///              (domain primitives, owned types, common configurations) for all derived contexts.
/// </summary>
public abstract class TemplatesCoreDbContextBase : DbContext
{
	protected TemplatesCoreDbContextBase(DbContextOptions options)
		: base(options)
	{
	}

	/// <summary>
	/// Applies common model configuration for all derived contexts.
	/// Derived contexts must call <c>base.OnModelCreating(modelBuilder)</c>.
	/// </summary>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyDomainPrimitives();
	}
}
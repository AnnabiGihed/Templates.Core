using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Templates.Core.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public abstract class UnitOfWork<TId> : IUnitOfWork<TId>
{
	#region Properties
	protected readonly DbContext _dbContext;
	protected readonly IOutboxRepository _outboxRepository;
	protected readonly IHttpContextAccessor _httpContextAccessor;
	protected readonly IDomainEventPublisher _domainEventPublisher;
	#endregion

	#region Constructors
	public UnitOfWork(DbContext dbContext, IHttpContextAccessor httpContextAccessor, IDomainEventPublisher domainEventPublisher, IOutboxRepository outboxRepository)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
		_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		_domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
	}
	#endregion

	#region IUnitOfWork Implementation
	public virtual async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			UpdateAuditableEntities();

			await SaveDomainEventsAsync(cancellationToken);

			var result = await _dbContext.SaveChangesAsync(cancellationToken);

			if(result > 0)			
				return Result.Success();
			else 
				return Result.Failure(new Error("SaveChangesError", "No changes were saved."));
		}
		catch (DbUpdateConcurrencyException concurrencyEx)
		{
			LogException(concurrencyEx);
			return Result.Failure(new Error("DbUpdateConcurrencyError", "A concurrency conflict occurred while saving changes."));
		}
		catch (DbUpdateException dbEx)
		{
			LogException(dbEx);
			var errorMessage = $": {dbEx.Message}";
			return Result.Failure(new Error("DatabaseError", "A database update error occurred"));
		}
		catch (Exception ex)
		{
			LogException(ex);
			return Result.Failure(new Error("UnexpectedError", "An unexpected error occurred."));
		}
	}
	#endregion

	#region Utilities
	protected void UpdateAuditableEntities()
	{
		foreach (var entityEntry in _dbContext.ChangeTracker.Entries<IAuditableEntity>())
		{
			if (entityEntry.Entity != null)
			{
				string currentUser = _httpContextAccessor.HttpContext?.User.Identity.Name ?? "System";
				bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity.IsAuthenticated ?? false;

				if (entityEntry.State == EntityState.Added)
				{
					var currentTime = DateTime.UtcNow;
					entityEntry.Entity.Audit.CreatedOnUtc = currentTime;
					entityEntry.Entity.Audit.CreatedBy = isAuthenticated ? currentUser : "System";
					entityEntry.Entity.Audit.ModifiedOnUtc = currentTime;
					entityEntry.Entity.Audit.ModifiedBy = isAuthenticated ? currentUser : "System";
				}
				else if (entityEntry.State == EntityState.Modified)
				{
					entityEntry.Entity.Audit.Modify(DateTime.UtcNow, isAuthenticated ? currentUser : "System");
				}
			}
		}
	}
	protected void LogException(Exception ex)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(ex.Message);
		if (ex.InnerException != null)
		{
			sb.AppendLine(ex.InnerException.Message);
		}
		Debug.WriteLine(sb.ToString());
	}
	protected async Task SaveDomainEventsAsync(CancellationToken cancellationToken)
	{
		// Get all aggregate roots with domain events
		var aggregatesWithEvents = _dbContext.ChangeTracker
			.Entries<IAggregateRoot>()
			.Where(e => e.Entity.GetDomainEvents().Any())
			.Select(e => e.Entity)
			.ToList();

		foreach (var aggregate in aggregatesWithEvents)
		{
			// Save each domain event to the outbox
			foreach (var domainEvent in aggregate.GetDomainEvents())
			{
				await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
			}

			// Clear domain events from the aggregate
			aggregate.ClearDomainEvents();
		}
	}
	#endregion
}

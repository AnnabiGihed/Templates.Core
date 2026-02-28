using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Domain.Primitives;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.DomainEventPublisher;

namespace Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Transaction-agnostic Unit of Work.
///              Persists business data and outbox messages in the ambient transaction
///              (transaction is owned by middleware).
/// </summary>
/// <typeparam name="TId">Strongly-typed ID used as DI scope key.</typeparam>
public abstract class UnitOfWork<TId> : IUnitOfWork<TId>
	where TId : IStronglyTypedId<TId>
{
	protected readonly DbContext _dbContext;
	protected readonly IHttpContextAccessor _httpContextAccessor;
	protected readonly IDomainEventPublisher _domainEventPublisher;

	protected UnitOfWork(
		DbContext dbContext,
		IHttpContextAccessor httpContextAccessor,
		IDomainEventPublisher domainEventPublisher)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		_domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
	}

	public virtual async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			UpdateAuditableEntities();

			// Persist domain events to the OUTBOX (not to broker)
			var persistEventsResult = await PersistDomainEventsToOutboxAsync(cancellationToken);
			if (persistEventsResult.IsFailure)
				return persistEventsResult;

			await _dbContext.SaveChangesAsync(cancellationToken);

			ClearDomainEvents();

			return Result.Success();
		}
		catch (DbUpdateConcurrencyException ex)
		{
			return Result.Failure(new Error("DbUpdateConcurrencyError", ex.Message));
		}
		catch (DbUpdateException ex)
		{
			return Result.Failure(new Error("DatabaseError", ex.Message));
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("UnexpectedError", ex.Message));
		}
	}

	protected virtual void UpdateAuditableEntities()
	{
		var now = DateTime.UtcNow;

		var identity = _httpContextAccessor.HttpContext?.User?.Identity;
		var actor = (identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(identity.Name))
			? identity.Name!
			: "System";

		foreach (var entry in _dbContext.ChangeTracker.Entries<IAuditableEntity>())
		{
			if (entry.State == EntityState.Added)
			{
				// Audit initialization for new entities (no direct property assignment)
				entry.Entity.SetAudit(AuditInfo.Create(now, actor));
			}
			else if (entry.State == EntityState.Modified)
			{
				// Audit update for modified entities
				entry.Entity.Audit.Modify(now, actor);
			}
		}
	}

	protected virtual async Task<Result> PersistDomainEventsToOutboxAsync(CancellationToken cancellationToken)
	{
		try
		{
			var aggregates = _dbContext.ChangeTracker
				.Entries<IAggregateRoot>()
				.Select(e => e.Entity)
				.Where(a => a.GetDomainEvents().Any())
				.ToList();

			foreach (var aggregate in aggregates)
			{
				foreach (var domainEvent in aggregate.GetDomainEvents())
				{
					var result = await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
					if (result.IsFailure)
						return result;
				}
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("DomainEventOutboxError", ex.Message));
		}
	}

	protected virtual void ClearDomainEvents()
	{
		foreach (var entry in _dbContext.ChangeTracker.Entries<IAggregateRoot>())
			entry.Entity.ClearDomainEvents();
	}
}

using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Templates.Core.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;
using Templates.Core.Infrastructure.Abstraction.Outbox.DomainEventPublisher;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.UnitOfWork;

public abstract class UnitOfWork<TContext>(DbContext dbContext, IHttpContextAccessor httpContextAccessor, IMessagePublisher messagePublisher, IOutboxRepository<TContext> outboxRepository, ILogger<UnitOfWork<TContext>> logger, IDomainEventPublisher domainEventPublisher) : IUnitOfWork<TContext> where TContext : DbContext
{
	#region Properties
	protected readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	protected readonly ILogger<UnitOfWork<TContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	protected readonly IMessagePublisher _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
	protected readonly IOutboxRepository<TContext> _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
	protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
	protected readonly IDomainEventPublisher _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
	#endregion

	#region IUnitOfWork Implementation
	/// <summary>
	/// Updates the auditable entities
	/// Saves the domain events
	/// Saves the changes to the database
	/// Triggers the outbox processing
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public virtual async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			UpdateAuditableEntities();

			var result = await SaveDomainEventsAsync(cancellationToken);

			if (!result.IsSuccess)
				return result;

			var dbContextResult = await _dbContext.SaveChangesAsync(cancellationToken);

			if (dbContextResult > 0)
				return await TriggerOutboxProcessingAsync(cancellationToken);
			else
				return Result.Failure(new Error("SaveChangesError", "No changes were saved."));
		}
		catch (DbUpdateConcurrencyException ex)
		{
			return Result.Failure(new Error("DbUpdateConcurrencyError", "A concurrency conflict occurred while saving changes, With Message" + ex.Message));
		}
		catch (DbUpdateException ex)
		{
			return Result.Failure(new Error("DatabaseError", "A database update error occurred With Message" + ex.Message));
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("UnexpectedError", "An unexpected error occurred. With Message" + ex.Message));
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
	protected async Task<Result> SaveDomainEventsAsync(CancellationToken cancellationToken)
	{
		try
		{
			var aggregatesWithEvents = _dbContext.ChangeTracker
				.Entries<IAggregateRoot>()
				.Where(e => e.Entity.GetDomainEvents().Any())
				.Select(e => e.Entity)
				.ToList();

			foreach (var aggregate in aggregatesWithEvents)
			{
				foreach (var domainEvent in aggregate.GetDomainEvents())
				{
					await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
				}

				aggregate.ClearDomainEvents();
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("DomainEventError", "An error occurred while saving domain events."));
		}
	}
	protected async Task<Result> TriggerOutboxProcessingAsync(CancellationToken cancellationToken)
	{
		try
		{
			var messages = await _outboxRepository.GetUnprocessedMessagesAsync(cancellationToken);

			if(messages == null || messages.Count == 0)
				return Result.Success();

			foreach (var message in messages)
			{
				var result = await _messagePublisher.PublishAsync(message.Payload);
				result = await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);		
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("OutboxProcessingError", "An error occurred while processing the outbox messages, With Message : " + ex.Message));
		}
	}
	#endregion
}

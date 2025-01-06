using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public abstract class UnitOfWork<TId> : IUnitOfWork<TId>
{
	protected readonly bool _enableOutbox;
	protected readonly bool _enableAuditing;
	protected readonly DbContext _dbContext;
	protected readonly IHttpContextAccessor _httpContextAccessor;
	public UnitOfWork(DbContext dbContext, IHttpContextAccessor httpContextAccessor, bool enableOutbox = false, bool enableAuditing = false)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		_enableOutbox = enableOutbox;
		_enableAuditing = enableAuditing;
	}

	public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			if (_enableOutbox) ConvertDomainEventsToOutboxMessages();
			if (_enableAuditing) UpdateAuditableEntities();

			await _dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(ex.Message);
			if (ex.InnerException != null)
			{
				sb.AppendLine(ex.InnerException.Message);
			}
			Debug.WriteLine(sb.ToString());
		}

	}

	/// <summary>
	/// This methods converts all raison (added) domain events of 
	/// an Aggregate to OutboxMessages and stores them to the
	/// OutboxMessage table in dbContext of the application.
	/// </summary>
	protected void ConvertDomainEventsToOutboxMessages()
	{
		var outboxMessages = _dbContext.ChangeTracker
			.Entries<IAggregateRoot>()
			.Select(x => x.Entity)
			.SelectMany(aggregateRoot =>
			{
				var domainEvents = aggregateRoot.GetDomainEvents();

				aggregateRoot.ClearDomainEvents();

				return domainEvents;
			})
			.Select(domainEvent => new OutboxMessage
			{
				Id = Guid.NewGuid(),
				OccurredOnUtc = DateTime.UtcNow,
				Type = domainEvent.GetType().Name,
				Content = JsonConvert.SerializeObject(
					domainEvent,
					new JsonSerializerSettings
					{
						TypeNameHandling = TypeNameHandling.All
					})
			})
			.ToList();

		_dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
	}

	/// <summary>
	/// This methods will set the audit props on add and update of every table in dbcontext
	/// IAuditableEntities should be instantiated in the constructor of the objects
	/// CurrentUser is from Httpcontext and if it comes from a domain event or null set it to the default "System".
	/// </summary>
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
}

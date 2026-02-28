using Pivot.Framework.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Pivot.Framework.Infrastructure.Abstraction.Outbox.Processor;

public interface IOutboxProcessor<TContext> where TContext : DbContext
{
	public Task<Result> ProcessOutboxMessagesAsync(CancellationToken cancellationToken);
}

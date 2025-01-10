using Templates.Core.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Templates.Core.Infrastructure.Abstraction.Outbox.Processor;

public interface IOutboxProcessor<TContext> where TContext : DbContext
{
	public Task<Result> ProcessOutboxMessagesAsync(CancellationToken cancellationToken);
}

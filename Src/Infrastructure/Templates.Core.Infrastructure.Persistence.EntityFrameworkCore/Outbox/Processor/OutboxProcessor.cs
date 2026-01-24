using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Outbox.Models;
using Templates.Core.Infrastructure.Abstraction.Outbox.Processor;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Processor;

public sealed class OutboxProcessor<TContext>(
	IOutboxRepository<TContext> outboxRepository,
	IMessagePublisher messagePublisher,
	TContext dbContext)
	: IOutboxProcessor<TContext>
	where TContext : DbContext
{
	private readonly IOutboxRepository<TContext> _outboxRepository = outboxRepository;
	private readonly IMessagePublisher _messagePublisher = messagePublisher;
	private readonly TContext _dbContext = dbContext;

	public async Task<Result> ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
	{
		try
		{
			var messages = await _outboxRepository.GetUnprocessedMessagesAsync(cancellationToken);

			if (messages.Count == 0)
				return Result.Success();

			foreach (var message in messages)
			{
				var publishResult = await _messagePublisher.PublishAsync(message);

				if (publishResult.IsFailure)
				{
					message.RetryCount++;
					await _dbContext.SaveChangesAsync(cancellationToken);
					return publishResult;
				}

				var markResult = await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
				if (markResult.IsFailure)
					return markResult;
			}

			await _dbContext.SaveChangesAsync(cancellationToken);
			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("OutboxProcessingError", ex.Message));
		}
	}
}

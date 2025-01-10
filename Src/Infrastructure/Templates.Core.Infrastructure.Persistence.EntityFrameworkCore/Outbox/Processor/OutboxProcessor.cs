using Templates.Core.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Abstraction.Outbox.Processor;
using Templates.Core.Infrastructure.Abstraction.Outbox.Repositories;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessagePublisher;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Processor;

public class OutboxProcessor<TContext>(IOutboxRepository<TContext> outboxRepository, IMessagePublisher messagePublisher) : IOutboxProcessor<TContext> where TContext : DbContext
{
	#region Properties
	protected readonly IMessagePublisher _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
	protected readonly IOutboxRepository<TContext> _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));

	#endregion

	#region IOutboxProcessor Implementation
	public async Task<Result> ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
	{
		try
		{
			var messages = await _outboxRepository.GetUnprocessedMessagesAsync(cancellationToken);
			if (messages == null || !messages.Any())
				return Result.Success();

			foreach (var message in messages)
			{
				var publishResult = await _messagePublisher.PublishAsync(message);

				if(publishResult.IsFailure)
					return publishResult;
				
				var result = await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
				
				if(result.IsFailure)
					return result;
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("OutboxProcessingError", $"Error processing outbox messages : {ex.Message}"));
		}
	}
	#endregion
}

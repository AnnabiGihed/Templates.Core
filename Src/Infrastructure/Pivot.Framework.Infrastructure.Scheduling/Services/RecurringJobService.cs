using Hangfire;
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Infrastructure.Abstraction.Scheduling.Services;
using Pivot.Framework.Infrastructure.Abstraction.Scheduling.Configurations;

namespace Pivot.Framework.Infrastructure.Scheduling.Services;

/// <summary>
/// Implementation of the IRecurringJobManager interface for managing Hangfire recurring jobs.
/// Supports parameterized job functions and operations like create, modify, delete, and run now.
/// </summary>
/// <typeparam name="TIdentifier">The type of the unique identifier for jobs.</typeparam>
/// <typeparam name="TParams">The type of the parameters for the job function.</typeparam>
/// <typeparam name="TValue">The return type of the job function result.</typeparam>
public class RecurringJobService<TIdentifier, TParams, TValue> : IRecurringJobService<TIdentifier, TParams, TValue>
{
	public Result RunJobNow(TIdentifier identifier)
	{
		try
		{
			var jobId = ConvertIdentifierToString(identifier);

			BackgroundJob.Enqueue(() => RecurringJob.TriggerJob(jobId));

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure<TValue>(new Error("Failed to run job immediately.", ex.Message));
		}
	}
	public Result DeleteJob(TIdentifier identifier)
	{
		try
		{
			var jobId = ConvertIdentifierToString(identifier);

			RecurringJob.RemoveIfExists(jobId);

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("Failed to delete job.", ex.Message));
		}
	}
	protected string ConvertIdentifierToString(TIdentifier identifier)
	{
		if (identifier == null)
			throw new ArgumentNullException(nameof(identifier));

		return identifier.ToString() ?? throw new InvalidOperationException("Identifier cannot be converted to a string.");
	}
	public Result CreateJob(TIdentifier identifier, RecurrenceConfig config, Func<Task<Result<TValue>>> jobFunction)
	{
		try
		{
			var jobId = ConvertIdentifierToString(identifier);

			RecurringJob.AddOrUpdate(jobId, () => jobFunction(), config.ToCronExpression());

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("Failed to create job.", ex.Message));
		}
	}
	public Result CreateJobWithParams(TIdentifier identifier, RecurrenceConfig config, TParams parameters, Func<TParams, Task<Result<TValue>>> jobFunction)
	{
		try
		{
			var jobId = ConvertIdentifierToString(identifier);

			RecurringJob.AddOrUpdate(jobId, () => jobFunction(parameters), config.ToCronExpression());

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error("Failed to create job with parameters.", ex.Message));
		}
	}
	public Result ModifyJobWithParams(TIdentifier identifier, RecurrenceConfig config, TParams parameters, Func<TParams, Task<Result<TValue>>> jobFunction)
	{
		return CreateJobWithParams(identifier, config, parameters, jobFunction);
	}
}

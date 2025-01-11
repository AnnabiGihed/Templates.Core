using Templates.Core.Domain.Shared;
using Templates.Core.Infrastructure.Abstraction.Scheduling.Configurations;

namespace Templates.Core.Infrastructure.Abstraction.Scheduling.Services;

/// <summary>
/// Interface for managing recurring jobs with Hangfire. Supports generic identifiers,
/// job results, and parameterized job functions.
/// </summary>
/// <typeparam name="TIdentifier">The type of the unique identifier for jobs.</typeparam>
/// <typeparam name="TParams">The type of the parameters for the job function.</typeparam>
/// <typeparam name="TValue">The return type of the job function result.</typeparam>
public interface IRecurringJobService<TIdentifier, TParams, TValue>
{
	/// <summary>
	/// Deletes an existing recurring job.
	/// </summary>
	Result DeleteJob(TIdentifier identifier);

	/// <summary>
	/// Executes a recurring job immediately.
	/// </summary>
	Result RunJobNow(TIdentifier identifier);
	/// <summary>
	/// Creates a new recurring job or updates an existing one, with or without parameters.
	/// </summary>
	Result CreateJob(TIdentifier identifier,	RecurrenceConfig config, Func<Task<Result<TValue>>> jobFunction);

	/// <summary>
	/// Creates or updates a recurring job with parameters.
	/// </summary>
	Result CreateJobWithParams(TIdentifier identifier, RecurrenceConfig config, TParams parameters, Func<TParams, Task<Result<TValue>>> jobFunction);

	/// <summary>
	/// Modifies an existing recurring job's schedule and parameters.
	/// </summary>
	Result ModifyJobWithParams(TIdentifier identifier, RecurrenceConfig config, TParams parameters, Func<TParams, Task<Result<TValue>>> jobFunction);
}
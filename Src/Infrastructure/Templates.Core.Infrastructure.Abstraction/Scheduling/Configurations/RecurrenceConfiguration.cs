using Templates.Core.Infrastructure.Abstraction.Scheduling.Enums;

namespace Templates.Core.Infrastructure.Abstraction.Scheduling.Configurations;

/// <summary>
/// Represents the configuration for recurring jobs, including the type and interval of recurrence.
/// Provides conversion to Hangfire-compatible Cron expressions.
/// </summary>
public class RecurrenceConfig
{
	/// <summary>
	/// The type of recurrence (e.g., hourly, daily, weekly, etc.).
	/// </summary>
	public RecurrenceType Type { get; set; }

	/// <summary>
	/// The interval for the recurrence (e.g., every X hours or days).
	/// </summary>
	public int Interval { get; set; }

	/// <summary>
	/// Converts the recurrence configuration into a Hangfire-compatible Cron expression.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported recurrence type is used.</exception>
	public string ToCronExpression()
	{
		return Type switch
		{
			RecurrenceType.Hourly => $"0 */{Interval} * * *", // Every 'Interval' hours.
			RecurrenceType.Daily => $"0 0 */{Interval} * *", // Every 'Interval' days at midnight.
			RecurrenceType.Weekly => $"0 0 * * 0/{Interval}", // Every 'Interval' weeks on Sunday at midnight.
			RecurrenceType.Monthly => $"0 0 1 */{Interval} *", // Every 'Interval' months on the 1st at midnight.
			RecurrenceType.Yearly => $"0 0 1 1 */{Interval}", // Every 'Interval' years on January 1st at midnight.
			_ => throw new ArgumentOutOfRangeException(nameof(Type), "Unsupported recurrence type.")
		};
	}
}

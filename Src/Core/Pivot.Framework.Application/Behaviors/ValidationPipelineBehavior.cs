using FluentValidation;
using MediatR;
using Pivot.Framework.Domain.Shared;

namespace Pivot.Framework.Application.Behaviors;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : MediatR pipeline behavior that executes FluentValidation validators for the request.
///              If validation errors exist, returns a validation failure Result (ValidationResult / ValidationResult{T}).
///              Otherwise continues to the next handler.
/// </summary>
/// <typeparam name="TRequest">The request type (command or query).</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="Result"/>.</typeparam>
public sealed class ValidationPipelineBehavior<TRequest, TResponse>
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
	where TResponse : Result
{
	private readonly IReadOnlyCollection<IValidator<TRequest>> _validators;

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> class.
	/// </summary>
	/// <param name="validators">Validators registered for the request type.</param>
	public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
	{
		_validators = validators?.ToArray() ?? throw new ArgumentNullException(nameof(validators));
	}

	/// <summary>
	/// Executes FluentValidation and returns validation failures as a ValidationResult.
	/// </summary>
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(next);

		if (_validators.Count == 0)
			return await next();

		var context = new ValidationContext<TRequest>(request);

		// Run all validators concurrently and await properly.
		var validationResults = await Task.WhenAll(
			_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

		var errors = validationResults
			.SelectMany(r => r.Errors)
			.Where(f => f is not null)
			.Select(f => new Error(
				code: f.PropertyName,   // You can change this to a standard code if you prefer.
				message: f.ErrorMessage))
			.Distinct()
			.ToArray();

		if (errors.Length > 0)
			return CreateValidationResult<TResponse>(errors);

		return await next();
	}

	/// <summary>
	/// Creates either a non-generic <see cref="ValidationResult"/> (when TResponse is Result)
	/// or a generic <see cref="ValidationResult{T}"/> (when TResponse is Result{T}).
	/// </summary>
	private static TResult CreateValidationResult<TResult>(Error[] errors)
		where TResult : Result
	{
		ArgumentNullException.ThrowIfNull(errors);

		// Non-generic Result
		if (typeof(TResult) == typeof(Result))
			return (ValidationResult.WithErrors(errors) as TResult)!;

		// Generic Result<T>
		if (typeof(TResult).IsGenericType &&
			typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
		{
			var valueType = typeof(TResult).GenericTypeArguments[0];

			var validationResultType = typeof(ValidationResult<>).MakeGenericType(valueType);

			var factory = validationResultType.GetMethod(
				nameof(ValidationResult<int>.WithErrors),
				new[] { typeof(Error[]) });

			if (factory is null)
				throw new InvalidOperationException($"Could not find WithErrors(Error[]) on {validationResultType.FullName}.");

			var instance = factory.Invoke(null, new object?[] { errors });
			return (TResult)instance!;
		}

		throw new InvalidOperationException(
			$"Unsupported response type '{typeof(TResult).FullName}'. " +
			$"ValidationPipelineBehavior supports Result and Result<T> only.");
	}
}

using ECommerceAPI.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace ECommerceAPI.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs FluentValidation validators
/// before any command/query handler executes.
/// If validation fails, the handler is never called.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Build a clean error message list
        var errors = failures
            .Select(f => f.ErrorMessage)
            .Distinct()
            .ToList();

        // Try to return Result<T> failure — works for all our handlers
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod(nameof(Result<object>.Failure),
                    new[] { typeof(string), typeof(int) });

            var errorMessage = string.Join(" | ", errors);
            var result = failureMethod!.Invoke(null, new object[] { errorMessage, 422 });
            return (TResponse)result!;
        }

        throw new ValidationException(failures);
    }
}

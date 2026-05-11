using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Common.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var message = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Return Result failure if TResponse is Result<T>
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var error = Error.Validation("validation.failed", message);
            var failureResult = typeof(Result<>)
                .MakeGenericType(responseType.GetGenericArguments()[0])
                .GetMethod(nameof(Result<object>.Failure))!
                .Invoke(null, [error]);
            return (TResponse)failureResult!;
        }

        throw new ValidationException(failures);
    }
}

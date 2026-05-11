using Cirth.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Cirth.Application.Common.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        if (response is Result<object> { IsFailure: true } result)
            logger.LogWarning("Request {RequestName} failed: {ErrorCode} — {ErrorMessage}",
                requestName, result.Error!.Code, result.Error.Message);
        else
            logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}

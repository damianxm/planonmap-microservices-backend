using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InfoMap.Shared.Infrastructure.Middleware;


internal sealed class OperationCancelledExceptionHandler(ILogger<OperationCancelledExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException)
            return ValueTask.FromResult(false);

        logger.LogDebug(
            "Request cancelled by client: {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        return ValueTask.FromResult(true);
    }
}

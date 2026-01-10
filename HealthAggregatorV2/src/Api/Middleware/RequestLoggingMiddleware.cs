using System.Diagnostics;

namespace HealthAggregatorV2.Api.Middleware;

/// <summary>
/// Request logging middleware.
/// Logs information about incoming requests and their processing time.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        _logger.LogDebug("Incoming {Method} request to {Path}", requestMethod, requestPath);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    "{Method} {Path} responded {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "{Method} {Path} responded {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
            else
            {
                _logger.LogInformation(
                    "{Method} {Path} responded {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
        }
    }
}

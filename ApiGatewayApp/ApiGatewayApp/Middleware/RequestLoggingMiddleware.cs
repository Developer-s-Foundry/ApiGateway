using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using Serilog;

namespace ApiGatewayApp.Middleware;

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
        var stopwatch = Stopwatch.StartNew();        // Create a scope with request info
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = context.TraceIdentifier ?? "unknown",
            ["RequestPath"] = context.Request.Path.ToString() ?? "unknown",
            ["RequestMethod"] = context.Request.Method ?? "unknown",
            ["RequestScheme"] = context.Request.Scheme ?? "unknown",
            ["RequestHost"] = context.Request.Host.ToString() ?? "unknown"
        });

        // Log request details
        LogRequestHeaders(context.Request);

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log the response details
            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "HTTP {RequestMethod} {RequestPath} failed with {ExceptionType}: {ExceptionMessage} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                ex.GetType().Name,
                ex.Message,
                stopwatch.ElapsedMilliseconds);

            throw; // Re-throw to allow other middleware to handle it
        }
    }
    private void LogRequestHeaders(HttpRequest request)
    {
        var headers = new Dictionary<string, string>();

        foreach (var header in request.Headers)
        {
            // Skip sensitive headers if needed
            if (IsSensitiveHeader(header.Key))
                continue;

            // Add the header with null check
            headers.Add(header.Key, header.Value.ToString() ?? string.Empty);
        }

        if (headers.Count != 0)
        {
            _logger.LogDebug("Request headers: {@Headers}", headers);
        }
    }

    private bool IsSensitiveHeader(string headerName)
    {
        // List of sensitive headers that shouldn't be logged
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "X-Api-Key"
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}

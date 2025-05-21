using ApiGatewayApp.Extensions;
using ApiGatewayApp.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

// Set up the bootstrap logger for startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Log startup information
    Log.Information("Starting up ApiGateway");

    var builder = WebApplication.CreateBuilder(args);

    // Configure services
    builder.Services.ConfigureProxyService();
    builder.Services.ConfigureAuthServices();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "api_gateway" });

    // Configure the HTTP request pipeline.

    // Configure OpenTelemetry
    builder.AddTelemetry();

    // Configure Serilog
    builder.AddLogging();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        c.RoutePrefix = string.Empty; // Serve the Swagger UI at the root
    });

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    // Only use HTTPS redirection in production environments
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // Add custom request logging middleware
    app.UseMiddleware<RequestLoggingMiddleware>();


    app.UseAuthentication();

    app.UseAuthorization(); main

    app.MapControllers();

    app.MapHealthChecks("/health");

    app.MapReverseProxy();

    Log.Information("Starting ApiGateway web host");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ApiGateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

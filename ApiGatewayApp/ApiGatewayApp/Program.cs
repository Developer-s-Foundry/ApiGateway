using ApiGatewayApp.Extensions;
using ApiGatewayApp.Middleware;
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
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure OpenTelemetry
    builder.AddTelemetry();

    // Configure Serilog
    builder.AddLogging();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    app.UseHttpsRedirection();

    // Add custom request logging middleware
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseAuthorization();

    app.MapControllers();

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

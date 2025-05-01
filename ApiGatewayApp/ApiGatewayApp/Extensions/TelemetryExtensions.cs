using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace ApiGatewayApp.Extensions;

public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {        // Get OpenTelemetry configuration
        var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = otelConfig["ServiceName"] ?? builder.Environment.ApplicationName;
        var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_ENDPOINT") ??
                           otelConfig["OtlpExporter:Endpoint"] ??
                           "http://localhost:4317";

        var otelProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL") ?? "grpc";
        var protocol = otelProtocol.ToLower() == "http"
            ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf
            : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddTelemetrySdk()
            .AddAttributes([
                new KeyValuePair<string, object>("environment", builder.Environment.EnvironmentName),
                new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
            ]);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metric =>
            {
                metric.SetResourceBuilder(resourceBuilder);

                // Add instrumentation
                metric.AddAspNetCoreInstrumentation();
                metric.AddHttpClientInstrumentation();                // Add OTLP exporter
                metric.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = protocol;
                });
            })
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder);

                // Add instrumentation
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.headers.user_agent", request.Headers.UserAgent);
                        activity.SetTag("http.request.host", request.Host.Value);
                    };
                });

                tracing.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithException = (activity, exception) =>
                    {
                        activity.SetTag("exception.message", exception.Message);
                        activity.SetTag("exception.stacktrace", exception.ToString());
                    };
                });

                // Add YARP instrumentation for reverse proxy
                tracing.AddSource("Yarp.ReverseProxy");                // Add OTLP exporter
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = protocol;
                });
            });

        // Add OpenTelemetry logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);

            // Configure log filtering
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;            // Add OTLP exporter for logs
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
                options.Protocol = protocol;
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures structured logging with Serilog
    /// </summary>
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        // Add Serilog for structured logging
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            // Create basic configuration
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().GetName().Name)
                .WriteTo.Console();

            // Add environment-specific configuration
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug();
            }
            else
            {
                configuration.MinimumLevel.Information();
            }
        });

        return builder;
    }
}

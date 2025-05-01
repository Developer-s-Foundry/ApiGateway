using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Diagnostics;

namespace ApiGatewayApp.Extensions;

public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {
        // Get OpenTelemetry configuration
        var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = otelConfig["ServiceName"] ?? builder.Environment.ApplicationName;

        // Try to get the OTEL endpoint from environment variables or use a fallback value
        var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_ENDPOINT") ??
                           otelConfig["OtlpExporter:Endpoint"] ??
                           "http://localhost:4317";

        // Attempt to parse OTEL_EXPORTER_OTLP_PROTOCOL, fallback to "grpc"
        var otelProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL") ?? "grpc";

        // If OTEL_EXPORTER_OTLP_PROTOCOL_FALLBACK is set, we'll try this protocol if primary fails
        var otelProtocolFallback = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL_FALLBACK");

        // Main protocol
        var protocol = otelProtocol.ToLower() == "http"
            ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf
            : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

        // If fallback protocol is specified, configure it as an option
        var useFallbackProtocol = !string.IsNullOrEmpty(otelProtocolFallback);

        var isInsecure = bool.Parse(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_INSECURE") ?? "true");

        // Configure network timeouts for better resilience
        var timeout = TimeSpan.FromSeconds(10);

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
                metric.SetResourceBuilder(resourceBuilder);                // Add instrumentation
                metric.AddAspNetCoreInstrumentation();
                metric.AddHttpClientInstrumentation();

                // Add OTLP exporter with batch processing configuration
                metric.AddOtlpExporter((options, readerOptions) =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = protocol;
                    options.TimeoutMilliseconds = (int)timeout.TotalMilliseconds;

                    if (isInsecure && protocol == OpenTelemetry.Exporter.OtlpExportProtocol.Grpc)
                    {
                        // For gRPC, we need to specify this differently
                        options.Headers = "Authorization=";
                    }

                    // Configure batch processing settings
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 60000;
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
                });                // Add YARP instrumentation for reverse proxy
                tracing.AddSource("Yarp.ReverseProxy");

                // Use batch processor for better performance and retry handling
                tracing.SetSampler(new AlwaysOnSampler());
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = protocol;
                    options.TimeoutMilliseconds = (int)timeout.TotalMilliseconds;

                    if (isInsecure && protocol == OpenTelemetry.Exporter.OtlpExportProtocol.Grpc)
                    {
                        // For gRPC, we need to specify this differently
                        options.Headers = "Authorization=";
                    }
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
                options.TimeoutMilliseconds = (int)timeout.TotalMilliseconds;

                if (isInsecure && protocol == OpenTelemetry.Exporter.OtlpExportProtocol.Grpc)
                {
                    // For gRPC, we need to specify this differently
                    options.Headers = "Authorization=";
                }
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

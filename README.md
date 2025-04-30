# API Gateway

This service acts as an API Gateway using YARP (Yet Another Reverse Proxy) to route traffic to various microservices.

## Features

- Routing API requests to appropriate microservices
- Distributed tracing with OpenTelemetry
- Metrics collection with OpenTelemetry
- Integration with Jaeger for visualizing traces

## Configuration

### OpenTelemetry

The application is configured to export telemetry data to Jaeger. The configuration is managed through:

1. Environment variables:
   - `OTEL_EXPORTER_ENDPOINT`: The endpoint for the OpenTelemetry collector (default: http://localhost:4317)

2. Application settings (`appsettings.json`):
   ```json
   "OpenTelemetry": {
     "ServiceName": "ApiGateway",
     "EnableConsoleExporter": false,
     "OtlpExporter": {
       "Endpoint": "http://localhost:4317",
       "Protocol": "Grpc"
     }
   }
   ```

## Running the Application

### Docker Compose

```bash
cd ApiGatewayApp
docker-compose up
```

After starting, you can:
- Access the API Gateway at http://localhost:8080
- Access Swagger UI at http://localhost:8080/swagger
- View traces in Jaeger UI at http://localhost:16686
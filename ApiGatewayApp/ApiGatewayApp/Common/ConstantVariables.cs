namespace ApiGatewayApp.Common;

public static class ConstantVariables
{
    internal static readonly string authServiceUrl = Environment.GetEnvironmentVariable("apiGatewayAuthServiceUrl")!;
    internal static readonly string apiKey = Environment.GetEnvironmentVariable("apiGatewayApiKey")!;
}

namespace ApiGatewayApp.Common;

public static class ConstantVariables
{
    internal static readonly string authServiceUrl = Environment.GetEnvironmentVariable("apiGatewayAuthServiceUrl")!;
    internal static readonly string apiKey = Environment.GetEnvironmentVariable("apiGatewayApiKey")!;
    internal static readonly string userSericeUrl = Environment.GetEnvironmentVariable("apiGatewayUserServiceUrl")!;
    internal static readonly string apiGatewayUrl = Environment.GetEnvironmentVariable("apiGatewayUrl")!;
}

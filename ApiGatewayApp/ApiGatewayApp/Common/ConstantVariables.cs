namespace ApiGatewayApp.Common;

public static class ConstantVariables
{
    public static readonly string authServiceUrl = Environment.GetEnvironmentVariable("apiGatewayAuthServiceUrl")!;
}

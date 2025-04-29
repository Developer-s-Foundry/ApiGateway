using ApiGatewayApp.Configs;

namespace ApiGatewayApp.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureProxyService(this IServiceCollection services)
    {
        services.AddReverseProxy()
            .LoadFromMemory(ProxyConfig.GetRoutes(), ProxyConfig.GetClusters());
    }
}

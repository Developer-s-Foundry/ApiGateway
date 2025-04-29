using Microsoft.AspNetCore.Authorization;
using Yarp.ReverseProxy.Configuration;
using static System.Net.WebRequestMethods;

namespace ApiGatewayApp.Configs;

public static class ProxyConfig
{
    private readonly static string userServiceUrl = "http://localhost:5001"; //Read env variable when obtained
    private readonly static string authServiceUrl = "http://localhost:5001"; //Read env variable when obtained

    public static IReadOnlyList<RouteConfig> GetRoutes()
    {
        return new[]
        {
            new RouteConfig
            {
                RouteId = "userRoute",
                ClusterId = "userCluster",
                Match = new RouteMatch
                {
                    Path = "/api/user/{**catch-all}" //switch to right path later
                },
                AuthorizationPolicy = "Anonymous" //Switch to specified auth when authentication is implemented
            },
            new RouteConfig
            {
                RouteId = "authRoute",
                ClusterId = "authCluster",
                Match = new RouteMatch
                {
                    Path = "/api/auth/{**catch-all}" //switch to right path later
                },
                AuthorizationPolicy = "Anonymous" //Switch to specified auth when authentication is implemented
            }
        };
    }
    public static IReadOnlyList<ClusterConfig> GetClusters()
    {
        return new[]
        {
            new ClusterConfig
            {
                ClusterId = "userCluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "userService", new DestinationConfig { Address = userServiceUrl } }
                }
            },
            new ClusterConfig
            {
                ClusterId = "authCluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "authService", new DestinationConfig { Address = authServiceUrl } }
                }
            }
        };
    }
}

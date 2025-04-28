using ApiGatewayApp.Common;
using ApiGatewayApp.Configs;
using Microsoft.IdentityModel.Tokens;

namespace ApiGatewayApp.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureProxyService(this IServiceCollection services)
    {
        services.AddReverseProxy()
            .LoadFromMemory(ProxyConfig.GetRoutes(), ProxyConfig.GetClusters());
    }

    public static void
        ConfigureAuthServices
        (this IServiceCollection services)
    {
        var validIssuer = Environment.GetEnvironmentVariable("REDTECHISSUER");
        var validAudience = Environment.GetEnvironmentVariable("REDTECHAUDIENCE");

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = ConstantVariables.authServiceUrl;
                options.RequireHttpsMetadata = false; // Set to true in production
                options.Audience = Environment.GetEnvironmentVariable("apiGatewayUserServiceUrl")!;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = ConstantVariables.authServiceUrl
                };
            });
    }
}

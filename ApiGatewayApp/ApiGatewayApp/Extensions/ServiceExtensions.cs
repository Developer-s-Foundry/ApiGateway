using ApiGatewayApp.Common;
using ApiGatewayApp.Configs;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Transforms;

namespace ApiGatewayApp.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureProxyService(this IServiceCollection services)
    {
        services.AddReverseProxy()
            .LoadFromMemory(ProxyConfig.GetRoutes(), ProxyConfig.GetClusters())
            .AddTransforms(builder =>
            {
                builder.AddRequestTransform(transformContext =>
                {
                    var apiKey = "";
                    var timestamp = "";
                    string concatenatedString = string.Join(':', new {apiKey, timestamp } );
                    var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes(concatenatedString)); ;
                    transformContext.HttpContext.Request.Headers.Add("X-API-GATEWAY-SIGNATURE", signature);
                });
            });
    }

    public static void
        ConfigureAuthServices
        (this IServiceCollection services)
    {
        var validIssuer = ConstantVariables.authServiceUrl;
        var validAudience = Environment.GetEnvironmentVariable("apiGatewayUserServiceUrl")!;

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = validIssuer;
                options.RequireHttpsMetadata = false; // Set to true in production
                options.Audience = "https://localhost:7073";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = validIssuer
                };
            });
    }
}
/*api_key = request. headers ["X-API-GATEWAY-KEY" ]
api_timestamp = request . headers ["X-API-GATEWAY-TIMESTAMP"]
api_signature = request . headers[ ]
user_id = request. headers ["X-USER-ID" ]
user_email = request. headers ["X-USER-EMAIL"]*/
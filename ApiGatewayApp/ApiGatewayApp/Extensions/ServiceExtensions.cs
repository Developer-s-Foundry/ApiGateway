using ApiGatewayApp.Common;
using ApiGatewayApp.Configs;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Yarp.ReverseProxy.Transforms;

namespace ApiGatewayApp.Extensions;

public static class ServiceExtensions
{
    public static void AddLoggingToConsole(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
        });
    }
    public static void ConfigureProxyService(this IServiceCollection services)
    {
        services.AddReverseProxy()
        .LoadFromMemory(ProxyConfig.GetRoutes(), ProxyConfig.GetClusters())
        .AddTransforms(builder =>
        {
            builder.AddRequestTransform(transformContext =>
            {
                string apiKey = ConstantVariables.apiKey;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string signature = GenerateSignature(apiKey, timestamp);

                string userId = transformContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                string userEmail = transformContext.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                builder.AddXForwarded(ForwardedTransformActions.Set);   
                transformContext.ProxyRequest.Headers.Add("X-API-GATEWAY-TIMESTAMP", timestamp);
                transformContext.ProxyRequest.Headers.Add("X-API-GATEWAY-SIGNATURE", signature);
                transformContext.ProxyRequest.Headers.Add("X-USER-ID", userId);
                transformContext.ProxyRequest.Headers.Add("X-USER-EMAIL", userEmail);

                return ValueTask.CompletedTask;
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

    private static string GenerateSignature(string apiKey, string timestamp)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{apiKey}:{timestamp}"));
        return Convert.ToBase64String(hash);
    }
}


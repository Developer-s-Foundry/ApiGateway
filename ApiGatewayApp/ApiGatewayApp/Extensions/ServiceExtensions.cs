using ApiGatewayApp.Common;
using ApiGatewayApp.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
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

                //string userId = transformContext.ProxyRequest.Headers.ProxyAuthorization[];
                string userId = string.Empty;
                string userEmail = string.Empty;

                if (transformContext.ProxyRequest.Headers.TryGetValues("Authorization", out var authHeaders))
                {
                    var authHeader = authHeaders.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
                    {
                        var token = headerValue.Parameter;
                        // Use System.IdentityModel.Tokens.Jwt to decode and read claims
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);
                        userId = jwt.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
                        userEmail = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    }
                }

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = validIssuer;
            options.RequireHttpsMetadata = false; 
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = validIssuer,

                ValidateAudience = true,
                ValidAudiences = new[] { ConstantVariables.apiGatewayUrl, ConstantVariables.userSericeUrl },

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
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
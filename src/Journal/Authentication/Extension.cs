using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Journal.Authentication;

public static class Extension
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache(); // dùng cho nonce trong HMAC

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Combined";
            options.DefaultAuthenticateScheme = "Combined";
            options.DefaultChallengeScheme = "Combined";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JWT:Issuer"],
                ValidAudience = configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                RoleClaimType = ClaimTypes.Role
            };
        })
        .AddScheme<AuthenticationSchemeOptions, HmacAuthenticationHandler>("HMAC", options => { })
        .AddPolicyScheme("Combined", "JWT or HMAC", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var hasBearer = context.Request.Headers["Authorization"].FirstOrDefault()?.StartsWith("Bearer ") == true;
                var hasHmac = context.Request.Headers.ContainsKey("X-Machine-Hash");

                if (hasHmac) return "HMAC";
                if (hasBearer) return JwtBearerDefaults.AuthenticationScheme;

                return JwtBearerDefaults.AuthenticationScheme;
            };
        });

        return services;
    }
}
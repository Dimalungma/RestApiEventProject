using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UsersService.Infrastructure.Security;

namespace UsersService.Presentation.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
                             .GetRequiredSection(JwtOptions.SectionName)
                             .Get<JwtOptions>()
                         ?? throw new InvalidOperationException(
                             "Не удалось загрузить настройки JWT.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }
}
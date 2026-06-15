using System.Text;
using ECS.Api.Common.Authorization;
using ECS.Infrastructure.Identity;
using ECS.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECS.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(ApiPolicies.AdminOnly, p => p.RequireRole(Roles.Admin))
            .AddPolicy(ApiPolicies.ManageFleet, p => p.RequireRole(Roles.Admin, Roles.FleetManager))
            .AddPolicy(ApiPolicies.ManageMaintenance, p => p.RequireRole(Roles.Admin, Roles.FleetManager, Roles.Technician))
            .AddPolicy(ApiPolicies.ManageInventory, p => p.RequireRole(Roles.Admin, Roles.WarehouseManager))
            .AddPolicy(ApiPolicies.ViewReports, p => p.RequireAuthenticatedUser());

        return services;
    }
}

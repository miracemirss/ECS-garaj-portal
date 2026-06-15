using ECS.Api.Services;
using ECS.Application.Common.Interfaces;
using Microsoft.OpenApi.Models;

namespace ECS.Api.Extensions;

/// <summary>
/// Registers API-layer concerns: controllers, Swagger, CORS, HttpContext access
/// and the current-user service.
/// </summary>
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ECS Fleet Maintenance & Inventory API",
                Version = "v1"
            });
            // JWT bearer security definition is added in the auth prompt.
        });

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy("Default", policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}

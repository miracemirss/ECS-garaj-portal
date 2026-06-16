using ECS.Api.Extensions;
using ECS.Api.Middleware;
using ECS.Application;
using ECS.Infrastructure;
using ECS.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Composition root: each layer wires its own services through a dedicated extension.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorizationPolicies();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// Lightweight, anonymous liveness probe for containers / load balancers.
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.MapControllers();

app.Run();

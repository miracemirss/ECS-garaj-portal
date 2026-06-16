# syntax=docker/dockerfile:1
# ---- Build context: ./backend ----

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy props + project files first for restore-layer caching.
COPY Directory.Build.props ./
COPY src/ECS.Domain/ECS.Domain.csproj                 src/ECS.Domain/
COPY src/ECS.Shared/ECS.Shared.csproj                 src/ECS.Shared/
COPY src/ECS.Application/ECS.Application.csproj        src/ECS.Application/
COPY src/ECS.Persistence/ECS.Persistence.csproj       src/ECS.Persistence/
COPY src/ECS.Infrastructure/ECS.Infrastructure.csproj src/ECS.Infrastructure/
COPY src/ECS.Api/ECS.Api.csproj                        src/ECS.Api/
RUN dotnet restore src/ECS.Api/ECS.Api.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish src/ECS.Api/ECS.Api.csproj -c Release -o /app /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# libfontconfig1: required by QuestPDF (SkiaSharp) to render PDFs on Linux.
# tzdata: Europe/Istanbul for localized report timestamps. curl: health check.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 tzdata curl \
 && rm -rf /var/lib/apt/lists/*

ENV TZ=Europe/Istanbul \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Storage (PDF reports) and logs live on mounted volumes in compose.
RUN mkdir -p /app/storage /app/logs
COPY --from=build /app ./

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ECS.Api.dll"]

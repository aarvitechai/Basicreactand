# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached unless project files change)
COPY Directory.Build.props RedStarVibe.slnx ./
COPY src/RedStarVibe.Api/RedStarVibe.Api.csproj src/RedStarVibe.Api/
COPY src/RedStarVibe.Application/RedStarVibe.Application.csproj src/RedStarVibe.Application/
COPY src/RedStarVibe.Domain/RedStarVibe.Domain.csproj src/RedStarVibe.Domain/
COPY src/RedStarVibe.Infrastructure/RedStarVibe.Infrastructure.csproj src/RedStarVibe.Infrastructure/
RUN dotnet restore src/RedStarVibe.Api/RedStarVibe.Api.csproj

COPY src/ src/
RUN dotnet publish src/RedStarVibe.Api/RedStarVibe.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RedStarVibe.Api.dll"]

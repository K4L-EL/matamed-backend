# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache bust arg (populated by Railway with the commit SHA)
ARG CACHE_BUST=1

# Restore dependencies first (separate layer for caching)
COPY Nex.Api.sln .
COPY Nex.Api/Nex.Api.csproj Nex.Api/
RUN dotnet restore

# Copy all source files (cache busted on every deploy)
COPY . .

# Build and publish
RUN dotnet publish Nex.Api/Nex.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Nex.Api.dll"]

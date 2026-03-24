# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

# Copy solution file and project files first (layer cache optimization)
COPY DocMigrate.slnx ./
COPY src/DocMigrate.API/DocMigrate.API.csproj             src/DocMigrate.API/
COPY src/DocMigrate.Application/DocMigrate.Application.csproj src/DocMigrate.Application/
COPY src/DocMigrate.Domain/DocMigrate.Domain.csproj       src/DocMigrate.Domain/
COPY src/DocMigrate.Infrastructure/DocMigrate.Infrastructure.csproj src/DocMigrate.Infrastructure/
COPY src/DocMigrate.Tests/DocMigrate.Tests.csproj         src/DocMigrate.Tests/

# Restore dependencies (cached if project files unchanged)
RUN dotnet restore

# Copy remaining source files
COPY . .

# Publish release build
RUN dotnet publish src/DocMigrate.API/DocMigrate.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for HEALTHCHECK
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose API port
EXPOSE 5029

# Configure ASP.NET Core to listen on port 5029
ENV ASPNETCORE_URLS=http://+:5029

# Health check — hits the /api/health endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:5029/api/health || exit 1

ENTRYPOINT ["dotnet", "DocMigrate.API.dll"]

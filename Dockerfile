# Multi-stage Dockerfile for StashPlayaVR API
# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /src

# Copy project files
COPY src/PlayaApiV2.csproj .
COPY src/PlayaApiV2.sln .

# Restore dependencies
RUN dotnet restore PlayaApiV2.csproj

# Copy source code
COPY src/ .

# Build the application
RUN dotnet build PlayaApiV2.csproj -c Release

# Publish the application
RUN dotnet publish PlayaApiV2.csproj -c Release --no-build -o /app/publish

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install required packages for SkiaSharp native libraries and curl for healthcheck
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libgdiplus \
    libc6-dev \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8890

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8890

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8890/api/playa/v2/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "PlayaApiV2.dll"]

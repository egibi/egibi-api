# ============================================
# Egibi API — Multi-stage Docker Build
# ============================================
# Build: docker build -t egibi-api .
# Run:   docker run -p 8080:8080 egibi-api
# ============================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and all project files first (layer caching for restore)
COPY egibi-api.sln ./
COPY egibi-api/egibi-api.csproj egibi-api/
COPY EgibiBinanceUsSdk/EgibiBinanceUsSdk.csproj EgibiBinanceUsSdk/
COPY EgibiCoinbaseSDK/EgibiCoinbaseSdk.csproj EgibiCoinbaseSDK/
COPY EgibiCoreLibrary/EgibiCoreLibrary.csproj EgibiCoreLibrary/
COPY EgibiGeoDateTimeDataLibrary/EgibiGeoDateTimeDataLibrary.csproj EgibiGeoDateTimeDataLibrary/
COPY EgibiQuestDB/EgibiQuestDbSdk.csproj EgibiQuestDB/
COPY EgibiStrategyLibrary/EgibiStrategyLibrary.csproj EgibiStrategyLibrary/

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore egibi-api.sln

# Copy everything else and build
COPY . .
RUN dotnet publish egibi-api/egibi-api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Security: run as non-root
RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

# Railway injects PORT env var; ASP.NET reads ASPNETCORE_URLS
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "egibi-api.dll"]

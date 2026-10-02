FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/IncidentOps.Domain/IncidentOps.Domain.csproj src/IncidentOps.Domain/
COPY src/IncidentOps.Application/IncidentOps.Application.csproj src/IncidentOps.Application/
COPY src/IncidentOps.Infrastructure/IncidentOps.Infrastructure.csproj src/IncidentOps.Infrastructure/
COPY src/IncidentOps.Api/IncidentOps.Api.csproj src/IncidentOps.Api/
RUN dotnet restore src/IncidentOps.Api/IncidentOps.Api.csproj

COPY src/ src/
RUN dotnet publish src/IncidentOps.Api/IncidentOps.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    OTEL_SERVICE_NAME=incident-ops-api

EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "IncidentOps.Api.dll"]

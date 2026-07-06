#build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

COPY src/RateShield.Core/RateShield.Core.csproj src/RateShield.Core/
COPY src/RateShield.Infrastructure/RateShield.Infrastructure.csproj src/RateShield.Infrastructure/
COPY src/RateShield.Gateway/RateShield.Gateway.csproj src/RateShield.Gateway/

RUN dotnet restore src/RateShield.Gateway/RateShield.Gateway.csproj

COPY src/ src/

RUN dotnet publish src/RateShield.Gateway/RateShield.Gateway.csproj --configuration Release --no-restore --output /app/publish

#runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system rateshield && adduser --system --ingroup rateshield --home /app rateshield

COPY --from=build --chown=rateshield:rateshield /app/publish .

USER rateshield

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health/ready || exit 1

ENTRYPOINT [ "dotnet", "RateShield.Gateway.dll" ]
#build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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

ENTRYPOINT [ "dotnet", "RateShield.Gateway.dll" ]
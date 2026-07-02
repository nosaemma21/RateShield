#build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/RateShield.SampleBackend/RateShield.SampleBackend.csproj src/RateShield.SampleBackend/

RUN dotnet restore src/RateShield.SampleBackend/RateShield.SampleBackend.csproj

COPY src/ src/

RUN dotnet publish src/RateShield.SampleBackend/RateShield.SampleBackend.csproj --configuration Release --no-restore --output /app/publish

#runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT [ "dotnet", "RateShield.SampleBackend.dll" ]
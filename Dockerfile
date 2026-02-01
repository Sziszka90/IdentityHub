FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IdentityHub.sln .
COPY src/IdentityHub.API/IdentityHub.API.csproj src/IdentityHub.API/
COPY src/IdentityHub.Core/IdentityHub.Core.csproj src/IdentityHub.Core/

RUN dotnet restore

COPY . .
WORKDIR /src/src/IdentityHub.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "IdentityHub.API.dll"]

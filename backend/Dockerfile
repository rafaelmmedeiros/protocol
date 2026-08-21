# Build with the SDK, ship on the runtime: the published image carries no compiler or sources.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, against project files only, so a source edit does not invalidate the
# restore layer.
COPY global.json ./
COPY Protocol.Api/Protocol.Api.csproj Protocol.Api/
RUN dotnet restore Protocol.Api/Protocol.Api.csproj

COPY Protocol.Api/ Protocol.Api/
RUN dotnet publish Protocol.Api/Protocol.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is only here so compose can health-check /health; the runtime image ships without it.
RUN apt-get update  && apt-get install -y --no-install-recommends curl  && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Protocol.Api.dll"]

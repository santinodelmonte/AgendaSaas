# API de AgendaSaaS (.NET 8). Build: docker build -t agendasaas-api backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AgendaSaaS/AgendaSaaS.csproj AgendaSaaS/
RUN dotnet restore AgendaSaaS/AgendaSaaS.csproj

COPY AgendaSaaS/ AgendaSaaS/
RUN dotnet publish AgendaSaaS/AgendaSaaS.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ReverseProxy__Enabled=true

# Health check para el hosting: GET /health (200 si la base responde).
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "AgendaSaaS.dll"]

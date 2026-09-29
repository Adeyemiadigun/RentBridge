# syntax=docker/dockerfile:1
# Render Dockerfile for RentBridge (.NET 10 + Postgres + Hangfire)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY RentBridge.Api/RentBridge.Api.csproj RentBridge.Api/
COPY RentBridge.Application/RentBridge.Application.csproj RentBridge.Application/
COPY RentBridge.Domain/RentBridge.Domain.csproj RentBridge.Domain/
COPY RentBridge.Infrastructure/RentBridge.Infrastructure.csproj RentBridge.Infrastructure/

RUN dotnet restore RentBridge.Api/RentBridge.Api.csproj

# Copy the rest of the source
COPY . .

RUN dotnet publish RentBridge.Api/RentBridge.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Npgsql needs the Kerberos/GSSAPI native lib for Postgres SSL negotiation.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app

COPY --from=build /app/publish .

# Render injects $PORT at runtime (default 10000). Do NOT hardcode ASPNETCORE_URLS here.
EXPOSE 10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Bind to Render's $PORT. Fall back to 10000 for local `docker run` without -e PORT.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet RentBridge.Api.dll"]

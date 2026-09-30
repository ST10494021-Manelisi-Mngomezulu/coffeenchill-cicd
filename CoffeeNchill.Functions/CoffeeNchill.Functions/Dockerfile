# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CoffeeNChill.Functions.csproj", "./"]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

COPY . .
RUN dotnet publish "CoffeeNChill.Functions.csproj" -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
WORKDIR /home/site/wwwroot

ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=build /app/publish .

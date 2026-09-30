# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

   COPY ["CoffeeNchill.Functions.csproj", "./"]
      RUN dotnet restore "CoffeeNchill.Functions.csproj"

COPY . .
   RUN dotnet publish "CoffeeNchill.Functions.csproj" -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
WORKDIR /home/site/wwwroot

ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=build /app/publish .

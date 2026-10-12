FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["CoffeeNChill.Functions.csproj", "./"]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "CoffeeNChill.Functions.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS runtime
WORKDIR /home/site/wwwroot

# Copy published output
COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 80
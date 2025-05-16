FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/NetSync.Cli/NetSync.Cli.csproj", "src/NetSync.Cli/"]
COPY ["src/NetSync/NetSync.csproj", "src/NetSync/"]
RUN dotnet restore "src/NetSync.Cli/NetSync.Cli.csproj"
COPY . .
WORKDIR "/src/src/NetSync.Cli"
RUN dotnet build "./NetSync.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NetSync.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetSync.Cli.dll"]

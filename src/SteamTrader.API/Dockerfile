FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SteamTrader.API/SteamTrader.API.csproj", "SteamTrader.API/"]
RUN dotnet restore "SteamTrader.API/SteamTrader.API.csproj"
COPY . .
WORKDIR "/src/SteamTrader.API"
RUN dotnet build "SteamTrader.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteamTrader.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamTrader.API.dll"]

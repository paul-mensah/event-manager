FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/EventsManager.Events.Api/EventsManager.Events.Api.csproj", "src/EventsManager.Events.Api/"]
COPY ["src/EventManager.Core/EventManager.Core.csproj", "src/EventManager.Core/"]
COPY ["src/EventManager.Data.Elasticsearch/EventManager.Data.Elasticsearch.csproj", "src/EventManager.Data.Elasticsearch/"]
COPY ["src/EventManager.Data.Redis/EventManager.Data.Redis.csproj", "src/EventManager.Data.Redis/"]
RUN dotnet restore "src/EventsManager.Events.Api/EventsManager.Events.Api.csproj"
COPY . .
WORKDIR "/src/EventsManager.Events.Api"
RUN dotnet build "EventsManager.Events.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventsManager.Events.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventsManager.Events.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/EventsManager.Invitations.Api/EventsManager.Invitations.Api.csproj", "src/EventsManager.Invitations.Api/"]
COPY ["src/EventManager.Core/EventManager.Core.csproj", "src/EventManager.Core/"]
COPY ["src/EventManager.Data.Elasticsearch/EventManager.Data.Elasticsearch.csproj", "src/EventManager.Data.Elasticsearch/"]
COPY ["src/EventManager.Data.Redis/EventManager.Data.Redis.csproj", "src/EventManager.Data.Redis/"]
COPY ["src/EventManager.Data.Sql/EventManager.Data.Sql.csproj", "src/EventManager.Data.Sql/"]
RUN dotnet restore "src/EventsManager.Invitations.Api/EventsManager.Invitations.Api.csproj"
COPY . .
WORKDIR "/src/src/EventsManager.Invitations.Api"
RUN dotnet build "EventsManager.Invitations.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventsManager.Invitations.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventsManager.Invitations.Api.dll"]

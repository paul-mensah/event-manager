FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EventsManager.Invitations.Api/EventsManager.Invitations.Api.csproj", "EventsManager.Invitations.Api/"]
COPY ["EventManager.Data.Redis/EventManager.Data.Redis.csproj", "EventManager.Data.Redis/"]
COPY ["EventManager.Data.Elasticsearch/EventManager.Data.Elasticsearch.csproj", "EventManager.Data.Elasticsearch/"]
COPY ["EventManager.Core/EventManager.Core.csproj", "EventManager.Core/"]
COPY ["EventManager.Data.Sql/EventManager.Data.Sql.csproj", "EventManager.Data.Sql/"]

RUN dotnet restore "EventsManager.Invitations.Api/EventsManager.Invitations.Api.csproj"

COPY . .

RUN dotnet new tool-manifest
RUN dotnet tool install dotnet-ef

WORKDIR "/src/EventsManager.Invitations.Api"
RUN dotnet build "EventsManager.Invitations.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventsManager.Invitations.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
EXPOSE 5468
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventsManager.Invitations.Api.dll"]

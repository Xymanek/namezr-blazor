FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Namezr.sln", "."]
COPY ["Directory.Packages.props", "."]

COPY ["Namezr/Namezr.csproj", "Namezr/"]
COPY ["Namezr.BackendServiceDefaults/Namezr.BackendServiceDefaults.csproj", "Namezr.BackendServiceDefaults/"]
COPY ["Namezr.Client/Namezr.Client.csproj", "Namezr.Client/"]
COPY ["Namzer.BlazorPortals/Namzer.BlazorPortals.csproj", "Namzer.BlazorPortals/"]
COPY ["X2CharacterPool/X2CharacterPool.csproj", "X2CharacterPool/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "Namezr/Namezr.csproj"

COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build "Namezr/Namezr.csproj" -c Release --no-restore

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "Namezr/Namezr.csproj" -c Release -o /app/publish --no-build --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "Namezr.dll"]

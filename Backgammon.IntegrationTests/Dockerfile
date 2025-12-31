# Alpine-based SDK for smaller test image (~450 MB vs ~850 MB)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
# Use BuildKit cache mount to cache NuGet packages across builds
RUN --mount=type=cache,id=nuget-tests,target=/root/.nuget/packages \
    dotnet restore
RUN dotnet build --no-restore
CMD ["dotnet", "test", "Backgammon.IntegrationTests/Backgammon.IntegrationTests.csproj"]

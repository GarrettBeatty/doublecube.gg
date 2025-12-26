FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build --no-restore
CMD ["dotnet", "test", "Backgammon.IntegrationTests/Backgammon.IntegrationTests.csproj"]

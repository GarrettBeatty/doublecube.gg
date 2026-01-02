# Build stage (Alpine-based for smaller image size)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer caching optimization)
COPY ["Backgammon.Server/Backgammon.Server.csproj", "Backgammon.Server/"]
COPY ["Backgammon.Core/Backgammon.Core.csproj", "Backgammon.Core/"]
COPY ["Backgammon.ServiceDefaults/Backgammon.ServiceDefaults.csproj", "Backgammon.ServiceDefaults/"]
# Use BuildKit cache mount to cache NuGet packages across builds
RUN --mount=type=cache,id=nuget-server,target=/root/.nuget/packages \
    dotnet restore "Backgammon.Server/Backgammon.Server.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Backgammon.Server"
RUN dotnet build "Backgammon.Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Backgammon.Server.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (Alpine-based for minimal size ~110 MB vs ~210 MB)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# Install wget for health checks
RUN apk add --no-cache wget

WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 5000
EXPOSE 5000

# Set ASP.NET Core to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000

# Use invariant globalization (recommended for APIs, saves ~30 MB)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Run the application
ENTRYPOINT ["dotnet", "Backgammon.Server.dll"]

# Build stage (Alpine-based for smaller image size)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

# Install Node.js and pnpm for Vite build
RUN apk add --no-cache nodejs npm && npm install -g pnpm

# Install TypedSignalR.Client.TypeScript generator tool
RUN dotnet tool install --global TypedSignalR.Client.TypeScript.Generator
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src

# Copy csproj files and restore dependencies (layer caching optimization)
COPY ["Backgammon.WebClient/Backgammon.WebClient.csproj", "Backgammon.WebClient/"]
COPY ["Backgammon.ServiceDefaults/Backgammon.ServiceDefaults.csproj", "Backgammon.ServiceDefaults/"]
COPY ["Backgammon.Server/Backgammon.Server.csproj", "Backgammon.Server/"]
COPY ["Backgammon.Core/Backgammon.Core.csproj", "Backgammon.Core/"]
COPY ["Backgammon.AI/Backgammon.AI.csproj", "Backgammon.AI/"]
COPY ["Backgammon.Analysis/Backgammon.Analysis.csproj", "Backgammon.Analysis/"]
COPY ["Backgammon.Plugins/Backgammon.Plugins.csproj", "Backgammon.Plugins/"]
# Use BuildKit cache mount to cache NuGet packages across builds
RUN --mount=type=cache,id=nuget-webclient,target=/root/.nuget/packages \
    dotnet restore "Backgammon.WebClient/Backgammon.WebClient.csproj" && \
    dotnet restore "Backgammon.Server/Backgammon.Server.csproj"

# Copy source code
COPY . .

# Generate TypeScript types from SignalR hub definitions
WORKDIR "/src/Backgammon.Server"
RUN --mount=type=cache,id=nuget-webclient,target=/root/.nuget/packages \
    dotnet build -c Release --no-restore && \
    dotnet tsrts --project Backgammon.Server.csproj --output ../Backgammon.WebClient/src/types/generated --asm

# Build and publish WebClient
WORKDIR "/src/Backgammon.WebClient"
RUN --mount=type=cache,id=nuget-webclient,target=/root/.nuget/packages \
    dotnet publish "Backgammon.WebClient.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (Alpine-based for minimal size ~110 MB vs ~210 MB)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# Install wget for health checks
RUN apk add --no-cache wget

WORKDIR /app
COPY --from=build /app/publish .

# Expose port 3000
EXPOSE 3000

# Set ASP.NET Core to listen on port 3000
ENV ASPNETCORE_URLS=http://+:3000

# Use invariant globalization (recommended for APIs, saves ~30 MB)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Run the application
ENTRYPOINT ["dotnet", "Backgammon.WebClient.dll"]

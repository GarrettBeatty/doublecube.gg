# Build stage (Debian-based)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer caching optimization)
COPY ["Backgammon.Server/Backgammon.Server.csproj", "Backgammon.Server/"]
COPY ["Backgammon.Core/Backgammon.Core.csproj", "Backgammon.Core/"]
COPY ["Backgammon.AI/Backgammon.AI.csproj", "Backgammon.AI/"]
COPY ["Backgammon.Analysis/Backgammon.Analysis.csproj", "Backgammon.Analysis/"]
COPY ["Backgammon.Plugins/Backgammon.Plugins.csproj", "Backgammon.Plugins/"]
COPY ["Backgammon.ServiceDefaults/Backgammon.ServiceDefaults.csproj", "Backgammon.ServiceDefaults/"]
# Use BuildKit cache mount to cache NuGet packages across builds
RUN --mount=type=cache,id=nuget-server,target=/root/.nuget/packages \
    dotnet restore "Backgammon.Server/Backgammon.Server.csproj"

# Copy source code and publish directly (skip separate build step)
COPY . .
WORKDIR "/src/Backgammon.Server"
RUN --mount=type=cache,id=nuget-server,target=/root/.nuget/packages \
    dotnet publish "Backgammon.Server.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (Debian-based with gnubg)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install gnubg for game analysis
# Note: Installing without X11/GUI dependencies (--no-install-recommends)
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    gnubg \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Add /usr/games to PATH (gnubg installs here on Debian)
ENV PATH="${PATH}:/usr/games"

# Verify gnubg installation
RUN gnubg --version

# Create gnubg configuration to disable sound (prevents ALSA errors in container)
RUN echo "set sound enable off" > /root/.gnubgrc

WORKDIR /app
COPY --from=build /app/publish .

# Expose port 5000
EXPOSE 5000

# Set ASP.NET Core to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000

# Run the application
ENTRYPOINT ["dotnet", "Backgammon.Server.dll"]

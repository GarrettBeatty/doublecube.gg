# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer caching optimization)
COPY ["Backgammon.Server/Backgammon.Server.csproj", "Backgammon.Server/"]
COPY ["Backgammon.Core/Backgammon.Core.csproj", "Backgammon.Core/"]
COPY ["Backgammon.ServiceDefaults/Backgammon.ServiceDefaults.csproj", "Backgammon.ServiceDefaults/"]
RUN dotnet restore "Backgammon.Server/Backgammon.Server.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Backgammon.Server"
RUN dotnet build "Backgammon.Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Backgammon.Server.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (minimal chiseled image for production)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 5000
EXPOSE 5000

# Set ASP.NET Core to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000

# Run the application
ENTRYPOINT ["dotnet", "Backgammon.Server.dll"]

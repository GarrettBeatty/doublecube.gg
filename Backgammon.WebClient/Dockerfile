# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer caching optimization)
COPY ["Backgammon.WebClient/Backgammon.WebClient.csproj", "Backgammon.WebClient/"]
COPY ["Backgammon.ServiceDefaults/Backgammon.ServiceDefaults.csproj", "Backgammon.ServiceDefaults/"]
RUN dotnet restore "Backgammon.WebClient/Backgammon.WebClient.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Backgammon.WebClient"
RUN dotnet build "Backgammon.WebClient.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Backgammon.WebClient.csproj" -c Release -o /app/publish --no-restore

# Runtime stage (minimal chiseled image for production)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 3000
EXPOSE 3000

# Set ASP.NET Core to listen on port 3000
ENV ASPNETCORE_URLS=http://+:3000

# Run the application
ENTRYPOINT ["dotnet", "Backgammon.WebClient.dll"]

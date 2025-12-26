# Running Backgammon with .NET Aspire

## What is .NET Aspire?

.NET Aspire is an opinionated stack for building cloud-ready, observable distributed applications. For this project, it:

- **Orchestrates all services**: Starts MongoDB, the SignalR server, and the web client together
- **Manages MongoDB in Docker**: Automatically downloads and runs MongoDB container
- **Provides observability**: Built-in telemetry, logging, and health checks
- **Handles service discovery**: Services automatically find each other
- **Simplifies development**: One command to run everything

## Prerequisites

1. **.NET 10 SDK** (already installed)
2. **Docker Desktop** - Required for MongoDB container
   ```bash
   # macOS
   brew install --cask docker
   
   # Or download from https://www.docker.com/products/docker-desktop
   ```
3. **Start Docker Desktop** before running

## Quick Start

### Option 1: Run with Aspire (Recommended)

```bash
# Navigate to the AppHost project
cd Backgammon.AppHost

# Run the orchestrator
dotnet run
```

This will:
1. Start MongoDB in a Docker container
2. Start the SignalR backend server (Backgammon.Web)
3. Start the Blazor WebAssembly frontend (Backgammon.WebClient)
4. Open the **Aspire Dashboard** in your browser

### Option 2: Run Standalone (Without Aspire)

If you don't have Docker or prefer manual setup:

```bash
# Terminal 1: Start MongoDB manually
brew services start mongodb-community

# Terminal 2: Start backend
cd Backgammon.Web
dotnet run

# Terminal 3: Start frontend
cd Backgammon.WebClient
dotnet run
```

## Aspire Dashboard

When you run with Aspire, the dashboard opens automatically at **http://localhost:15001**

The dashboard provides:
- **Resources**: View all running services (MongoDB, API, WebClient)
- **Console Logs**: Real-time logs from all services
- **Traces**: Distributed tracing across services
- **Metrics**: Performance metrics, request counts, response times
- **Environment**: Service URLs and configuration

### Key Features

**View Service Endpoints:**
- MongoDB: Automatically assigned port (e.g., `mongodb://localhost:27017`)
- API Server: `http://localhost:5xxx`
- WebClient: `http://localhost:5yyy`

**Monitor MongoDB:**
- Click on "mongodb" resource → Click "mongo-express" endpoint
- Opens MongoExpress web UI to browse database

**View Logs:**
- Click "Console Logs" tab
- Select service to view real-time logs
- Filter by log level (Info, Warning, Error)

**Distributed Tracing:**
- Click "Traces" tab
- See complete request flow from frontend → SignalR → MongoDB
- Identify performance bottlenecks

## Architecture with Aspire

```
┌─────────────────────────────────────────────────┐
│          Backgammon.AppHost                     │
│  (Orchestrator - coordinates everything)        │
└─────────────────────────────────────────────────┘
                      │
          ┌───────────┼───────────┐
          │           │           │
          ▼           ▼           ▼
    ┌─────────┐  ┌────────┐  ┌──────────┐
    │ MongoDB │  │  Web   │  │WebClient │
    │ (Docker)│  │ Server │  │ (Blazor) │
    └─────────┘  └────────┘  └──────────┘
          ▲           │
          └───────────┘
        Database
       Connection
```

## What Aspire Does

### 1. MongoDB Container Management

**AppHost.cs:**
```csharp
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("backgammon-mongodb-data")  // Data persists across restarts
    .WithMongoExpress();  // Web UI for MongoDB
```

Aspire:
- Downloads MongoDB Docker image if not present
- Starts container on random port
- Creates persistent volume for data
- Injects connection string into services
- Provides MongoExpress UI for database browsing

### 2. Service Discovery

**Web/Program.cs:**
```csharp
builder.AddMongoDBClient("mongodb");  // "mongodb" matches AppHost resource name
```

The `AddMongoDBClient()` automatically:
- Discovers MongoDB connection string from Aspire
- Configures health checks
- Adds telemetry/tracing
- Falls back to `appsettings.json` when not using Aspire

### 3. Observability

**ServiceDefaults project** adds:
- OpenTelemetry tracing
- Metrics collection
- Health check endpoints
- Resilience (retry policies)
- Service discovery

## Configuration Files

### AppHost.cs
```csharp
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("backgammon-mongodb-data")
    .WithMongoExpress();

var database = mongodb.AddDatabase("backgammon");

var apiService = builder.AddProject<Projects.Backgammon_Web>("backgammon-api")
    .WithReference(database)  // Injects MongoDB connection
    .WaitFor(mongodb);       // Ensures MongoDB starts first

builder.AddProject<Projects.Backgammon_WebClient>("backgammon-webclient")
    .WithReference(apiService)  // Injects API URL
    .WaitFor(apiService);      // Starts after API is ready
```

### Web/Program.cs
```csharp
// Adds telemetry, health checks, service discovery
builder.AddServiceDefaults();

// Automatically discovers MongoDB from Aspire
builder.AddMongoDBClient("mongodb");

// Maps health check endpoints at /health, /alive, /ready
app.MapDefaultEndpoints();
```

## Project Structure

```
Backgammon/
├── Backgammon.AppHost/          # 🎯 Aspire orchestrator
│   ├── AppHost.cs               # Service configuration
│   └── Backgammon.AppHost.csproj
├── Backgammon.ServiceDefaults/  # 📊 Shared Aspire config
│   └── Extensions.cs            # Telemetry, health, resilience
├── Backgammon.Web/              # 🌐 SignalR backend
├── Backgammon.WebClient/        # 💻 Blazor frontend
└── Backgammon.Core/             # 🎲 Game engine
```

## Development Workflow

### 1. Code Changes

Edit any file (e.g., `GameEngine.cs`) and save.

**With Aspire:**
- Dashboard shows services restarting automatically
- Hot reload applies changes instantly

**Without Aspire:**
- Manually restart `dotnet run` in each terminal

### 2. Database Changes

**View data:**
- Aspire Dashboard → "mongodb" → "mongo-express" endpoint
- Or use `mongosh` CLI

**Persist data:**
- Data stored in Docker volume `backgammon-mongodb-data`
- Survives container restarts
- To reset: `docker volume rm backgammon-mongodb-data`

### 3. Debugging

**Attach debugger:**
```bash
# Run with Aspire in debug mode
cd Backgammon.AppHost
dotnet run --launch-profile https
```

Then attach VS Code debugger to processes.

**View traces:**
- Aspire Dashboard → Traces tab
- Click on any request to see full path
- See timing for SignalR, MongoDB queries, etc.

## Environment Variables

Aspire automatically sets:

```bash
# Backend (Backgammon.Web)
ConnectionStrings__mongodb=mongodb://localhost:27017
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Frontend (Backgammon.WebClient)
services__backgammon-api__http__0=http://localhost:5xxx
```

Override in `launchSettings.json` if needed.

## Troubleshooting

### Docker Not Running

**Error:**
```
Cannot connect to Docker daemon
```

**Fix:**
- Open Docker Desktop
- Wait for it to fully start (whale icon in menu bar)
- Run `dotnet run` again

### Port Conflicts

**Error:**
```
Address already in use
```

**Fix:**
- Aspire uses random ports by default
- Or stop conflicting service
- Or edit `AppHost.cs`:
  ```csharp
  .WithEndpoint(port: 5432, scheme: "http", name: "api")
  ```

### MongoDB Not Connecting

**Fix:**
- Check Aspire Dashboard → "mongodb" resource shows green
- Verify connection string in logs
- Check Docker Desktop shows `mongo` container running

### Missing Dependencies

**Error:**
```
Aspire.Hosting.AppHost not found
```

**Fix:**
```bash
dotnet workload update
dotnet restore
```

## Production Deployment

For production, don't use AppHost. Instead:

1. **MongoDB**: Use MongoDB Atlas or managed service
2. **Backend**: Deploy `Backgammon.Web` to Azure App Service / AWS ECS
3. **Frontend**: Deploy `Backgammon.WebClient` static files to CDN

Update `appsettings.Production.json`:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://username:password@cluster.mongodb.net",
    "DatabaseName": "backgammon"
  }
}
```

## Benefits of Aspire

| Without Aspire | With Aspire |
|---------------|-------------|
| 3 terminal windows | 1 command |
| Manual MongoDB install | Automatic Docker container |
| Manual connection strings | Service discovery |
| No observability | Built-in dashboard |
| Manual health checks | Automatic /health endpoints |
| Scattered logs | Unified log viewer |

## Next Steps

- **View Dashboard**: http://localhost:15001
- **Play Game**: Check dashboard for WebClient URL
- **Monitor Performance**: Dashboard → Metrics tab
- **Query Database**: Dashboard → "mongodb" → "mongo-express"

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [MongoDB in Aspire](https://learn.microsoft.com/dotnet/aspire/database/mongodb-integration)
- [Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)

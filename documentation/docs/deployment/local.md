---
sidebar_position: 1
---

# Local Development

Run DoubleCube.gg on your local machine for development.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) with pnpm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Option 1: Using .NET Aspire (Recommended)

The easiest way to run everything:

```bash
cd Backgammon.AppHost
dotnet run
```

This automatically starts:
- **DynamoDB Local** (Docker container, port 8000)
- **Redis** (Docker container, port 6379)
- **Backend Server** (port 5000)
- **Frontend Dev Server** (port 3000)

The Aspire Dashboard opens automatically with:
- Service status
- Logs
- Traces
- Metrics

## Option 2: Manual Start

### Start DynamoDB Local

```bash
docker run -p 8000:8000 amazon/dynamodb-local
```

### Start Redis (Optional)

Required for multi-server scenarios:

```bash
docker run -p 6379:6379 redis
```

### Start Backend Server

```bash
cd Backgammon.Server
dotnet run
```

Server runs on `http://localhost:5000`

### Start Frontend

```bash
cd Backgammon.WebClient
pnpm dev
```

Frontend runs on `http://localhost:3000`

## Environment Variables

### Server Configuration

Create `Backgammon.Server/appsettings.Development.json`:

```json
{
  "DynamoDb": {
    "TableName": "backgammon-local"
  },
  "Jwt": {
    "Secret": "your-development-secret-key-at-least-32-chars",
    "Issuer": "BackgammonServer",
    "Audience": "BackgammonClient"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Frontend Configuration

The frontend automatically connects to `http://localhost:5000` in development.

## Database Initialization

The DynamoDB table is created automatically on server startup when `AWS_ENDPOINT_URL_DYNAMODB` is set (local development).

To manually verify the table:

```bash
aws dynamodb list-tables --endpoint-url http://localhost:8000
```

## Debugging

### Backend

- **Visual Studio**: F5 to start with debugger
- **VS Code**: Use the C# Dev Kit launch configuration
- **Rider**: Standard .NET debugging

### Frontend

- Browser DevTools for React components
- React DevTools extension for component inspection
- Network tab for SignalR messages

### SignalR Debugging

Enable detailed errors in development:

```csharp
signalRBuilder.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
```

View WebSocket frames in browser Network tab → WS tab.

## Common Issues

### Port Already in Use

```bash
# Find process using port
lsof -i :5000

# Kill process
kill -9 <PID>
```

### DynamoDB Connection Failed

Ensure Docker is running and the container is started:

```bash
docker ps
```

### CORS Errors

Development mode allows all origins. If you see CORS errors, ensure you're running in Development environment.

### JWT Errors

Ensure your JWT secret in appsettings is at least 32 characters.

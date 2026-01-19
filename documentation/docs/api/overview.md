---
sidebar_position: 1
---

# API Overview

DoubleCube.gg exposes two types of APIs:

1. **REST API** - Traditional HTTP endpoints for CRUD operations
2. **SignalR Hub** - Real-time WebSocket communication for gameplay

## API Access

### Base URLs

- **Development**: `http://localhost:5000`
- **Production**: `https://api.doublecube.gg`

### Authentication

Most endpoints require JWT authentication:

```http
Authorization: Bearer <token>
```

For SignalR connections, pass the token in the query string:

```
wss://api.doublecube.gg/gamehub?access_token=<token>
```

## REST vs SignalR

| Use Case | API |
|----------|-----|
| User registration/login | REST |
| Profile updates | REST |
| Friend management | REST (or SignalR) |
| Game actions (moves, dice) | SignalR |
| Real-time game state | SignalR |
| Chat messages | SignalR |

## Swagger Documentation

The server exposes Swagger UI at `/swagger`:

- **Development**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

This provides:
- Interactive API testing
- Request/response schemas
- SignalR hub method documentation

## TypeScript Types

The frontend uses auto-generated TypeScript types from the server interfaces:

```bash
cd Backgammon.WebClient
pnpm generate:signalr
```

This creates strongly-typed method signatures for all SignalR operations.

## Error Handling

### REST API Errors

```json
{
  "error": "Description of what went wrong",
  "code": "ERROR_CODE"
}
```

### SignalR Errors

The server sends error events:

```typescript
connection.on('Error', (message: string) => {
  console.error('Server error:', message);
});
```

## Rate Limiting

API endpoints are rate-limited:
- REST: 100 requests/minute per IP
- SignalR: No explicit limit (connection-based)

## CORS

The server allows cross-origin requests from configured domains:

- Development: All origins allowed
- Production: Only `doublecube.gg` and subdomains

## Next Steps

- [REST API Reference](/api/rest-api) - HTTP endpoints
- [SignalR Hub Reference](/api/signalr-hub) - Real-time methods

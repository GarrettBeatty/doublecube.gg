---
sidebar_position: 3
---

# Development Guide

Learn the development workflow and common tasks.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build Backgammon.Server

# Build in release mode
dotnet build -c Release
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test Backgammon.Tests
```

## Frontend Development

### Development Server

```bash
cd Backgammon.WebClient
pnpm dev
```

Hot Module Replacement (HMR) is enabled for instant updates.

### TypeScript Checking

```bash
pnpm tsc --noEmit
```

### Regenerate SignalR Types

After modifying `IGameHub` or `IGameHubClient`:

```bash
pnpm generate:signalr
```

### Build for Production

```bash
pnpm build
```

Output goes to `wwwroot/` directory.

## Code Quality

### Backend (StyleCop)

The project enforces StyleCop rules:

- One type per file
- File name matches class name
- XML documentation on public members
- Usings outside namespace

See the StyleCop section in the project's CLAUDE.md file for details.

### Frontend (ESLint)

```bash
cd Backgammon.WebClient
pnpm lint
```

## Database (DynamoDB Local)

When running via Aspire, DynamoDB Local starts automatically.

For manual setup:

```bash
docker run -p 8000:8000 amazon/dynamodb-local
```

The table is auto-created on server startup.

## Common Tasks

### Add a New SignalR Method

1. Add method signature to `IGameHub.cs`
2. Implement in `GameHub.cs`
3. Regenerate TypeScript types: `pnpm generate:signalr`
4. Use in frontend via `connection.invoke('MethodName', args)`

### Add a New REST Endpoint

1. Add endpoint in `Program.cs` using minimal API syntax
2. Apply `.RequireAuthorization()` if auth needed
3. Apply `.RequireCors(selectedCorsPolicy)`

### Add a New Page

1. Create component in `src/pages/`
2. Add route in `App.tsx`
3. Update navigation if needed

## Debugging

### Backend

Set breakpoints in Visual Studio, Rider, or VS Code with the C# extension.

### Frontend

Use browser DevTools. React DevTools extension recommended.

### SignalR

Enable detailed errors in development:

```csharp
options.EnableDetailedErrors = builder.Environment.IsDevelopment();
```

View SignalR logs in browser console.

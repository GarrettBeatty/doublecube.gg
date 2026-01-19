---
sidebar_position: 1
---

# Installation

This guide covers how to set up the DoubleCube.gg development environment.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) with pnpm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for DynamoDB Local)

## Clone the Repository

```bash
git clone https://github.com/garrett/backgammon.git
cd backgammon
```

## Install Dependencies

### Backend

```bash
dotnet restore
```

### Frontend

```bash
cd Backgammon.WebClient
pnpm install
```

## Verify Installation

Build the entire solution to ensure everything is set up correctly:

```bash
dotnet build
```

Run the test suite:

```bash
dotnet test
```

## IDE Setup

### Visual Studio / Rider

Open `Backgammon.sln` at the project root.

### VS Code

Install the recommended extensions:
- C# Dev Kit
- ESLint
- Tailwind CSS IntelliSense

## Next Steps

- [Quick Start](/getting-started/quick-start) - Run the application
- [Development Guide](/getting-started/development) - Learn the development workflow

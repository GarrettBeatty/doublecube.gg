var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for game state persistence
var postgres = builder.AddPostgres("pg")
    .WithDataVolume();

var postgresDb = postgres.AddDatabase("Postgres");

// Redis for SignalR backplane and HybridCache distributed layer
var redis = builder.AddRedis("redis");

// Orleans cluster — development mode: single-VM clustering, grain storage persisted to Postgres.
// The "Default" grain storage provider is configured in Backgammon.Server's Program.cs as
// AdoNet against the same Postgres database (schema bootstrapped by OrleansSchemaInitializer).
// PubSubStore stays in memory since Orleans Streams aren't used.
// Swap WithDevelopmentClustering() → WithClustering(redis) for multi-node scale-out.
var orleans = builder.AddOrleans("default")
    .WithDevelopmentClustering()
    .WithMemoryGrainStorage("PubSubStore");

// GNU Backgammon analysis service (Docker container)
var gnubgService = builder.AddDockerfile("gnubg-service", "../gnubg-service")
    .WithHttpEndpoint(targetPort: 8080, name: "http");

// SignalR backend server
var apiService = builder.AddProject<Projects.Backgammon_Server>("backgammon-api")
    .WithReference(postgresDb)
    .WithReference(redis)
    .WithReference(orleans)
    .WithEnvironment("Gnubg__ServiceUrl", gnubgService.GetEndpoint("http"))
    .WaitFor(postgresDb)
    .WaitFor(redis)
    .WaitFor(gnubgService);

// Blazor WebAssembly frontend - gets API URL via service discovery
builder.AddProject<Projects.Backgammon_WebClient>("backgammon-webclient")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

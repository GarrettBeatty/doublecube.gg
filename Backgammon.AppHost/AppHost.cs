var builder = DistributedApplication.CreateBuilder(args);

// MongoDB container - automatically downloads and runs in Docker
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("backgammon-mongodb-data")  // Persist data across restarts
    .WithMongoExpress();  // Optional: Web UI for MongoDB

// Get the database from MongoDB
var database = mongodb.AddDatabase("backgammon");

// SignalR backend server - Aspire assigns port automatically
var apiService = builder.AddProject<Projects.Backgammon_Server>("backgammon-api")
    .WithReference(database)
    .WaitFor(mongodb);

// Blazor WebAssembly frontend - gets API URL via service discovery
builder.AddProject<Projects.Backgammon_WebClient>("backgammon-webclient")
    .WithExternalHttpEndpoints()  // Make it accessible from browser
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

using Aspire.Hosting.AWS;

var builder = DistributedApplication.CreateBuilder(args);

// DynamoDB Local for development with persistence
var dynamoDb = builder.AddAWSDynamoDBLocal("dynamodb-local");

// Redis for SignalR backplane (enables real-time updates across multiple server instances)
var redis = builder.AddRedis("redis");

// GNU Backgammon analysis service (Docker container)
var gnubgService = builder.AddDockerfile("gnubg-service", "../gnubg-service")
    .WithHttpEndpoint(targetPort: 8080, name: "http");

// SignalR backend server - Aspire assigns port automatically
var apiService = builder.AddProject<Projects.Backgammon_Server>("backgammon-api")
    .WithReference(dynamoDb)
    .WithReference(redis)
    .WithEnvironment("Gnubg__ServiceUrl", gnubgService.GetEndpoint("http"))
    .WaitFor(dynamoDb)
    .WaitFor(redis)
    .WaitFor(gnubgService);

// Blazor WebAssembly frontend - gets API URL via service discovery
builder.AddProject<Projects.Backgammon_WebClient>("backgammon-webclient")
    .WithExternalHttpEndpoints() // Make it accessible from browser
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

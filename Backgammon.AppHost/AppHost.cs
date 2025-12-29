using Aspire.Hosting.AWS;

var builder = DistributedApplication.CreateBuilder(args);

// DynamoDB Local for development with persistence
var dynamoDb = builder.AddAWSDynamoDBLocal("dynamodb-local");

// SignalR backend server - Aspire assigns port automatically
var apiService = builder.AddProject<Projects.Backgammon_Server>("backgammon-api")
    .WithReference(dynamoDb)
    .WaitFor(dynamoDb);

// Blazor WebAssembly frontend - gets API URL via service discovery
builder.AddProject<Projects.Backgammon_WebClient>("backgammon-webclient")
    .WithExternalHttpEndpoints()  // Make it accessible from browser
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

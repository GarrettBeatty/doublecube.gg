using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Xunit;

namespace Backgammon.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Test.json");
        });
    }
}

public class SignalRGameFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SignalRGameFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        CleanDatabase();
    }

    private void CleanDatabase()
    {
        // Drop the test database before each test run
        var mongoClient = new MongoClient("mongodb://mongodb:27017");
        mongoClient.DropDatabase("backgammon_test");
    }

    private HubConnection CreateHubConnection(HttpClient client)
    {
        // Ensure correct URL: avoid double slash, use Uri constructor
        var serverUri = new Uri(client.BaseAddress, "gamehub");
        return new HubConnectionBuilder()
            .WithUrl(serverUri, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .WithAutomaticReconnect()
            .Build();
    }

    [Fact]
    public async Task CanCreateJoinAndPlayGame()
    {
        var client = _factory.CreateClient();
        var player1 = CreateHubConnection(client);
        var player2 = CreateHubConnection(client);

        await player1.StartAsync();
        Assert.Equal(HubConnectionState.Connected, player1.State);
        await player2.StartAsync();
        Assert.Equal(HubConnectionState.Connected, player2.State);

        // Diagnostics: subscribe to connection events
        player1.Closed += async (error) =>
        {
            Console.WriteLine($"Player1 closed: {error}");
            await Task.CompletedTask;
        };
        player2.Closed += async (error) =>
        {
            Console.WriteLine($"Player2 closed: {error}");
            await Task.CompletedTask;
        };
        player1.Reconnecting += async (error) =>
        {
            Console.WriteLine($"Player1 reconnecting: {error}");
            await Task.CompletedTask;
        };
        player2.Reconnecting += async (error) =>
        {
            Console.WriteLine($"Player2 reconnecting: {error}");
            await Task.CompletedTask;
        };

        // Example: create/join game, play moves
        string player1Id = "player1";
        string player2Id = "player2";
        string gameId = null;

        // Listen for WaitingForOpponent to get gameId
        player1.On<string>("WaitingForOpponent", id => gameId = id);
        await player1.InvokeAsync("JoinGame", player1Id, null);
        await Task.Delay(500); // Wait for event
        Assert.False(string.IsNullOrEmpty(gameId));

        await player2.InvokeAsync("JoinGame", player2Id, gameId);
        // Add more assertions and simulate moves as needed

        await player1.StopAsync();
        await player2.StopAsync();
    }
}

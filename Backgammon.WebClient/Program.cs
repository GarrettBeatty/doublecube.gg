var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire integration
builder.AddServiceDefaults();

var app = builder.Build();

// Map default health endpoints
app.MapDefaultEndpoints();

// Serve static files (HTML, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

// Expose API URL to browser via endpoint
app.MapGet("/api/config", (IConfiguration config) =>
{
    // Try multiple configuration key formats for Aspire service discovery
    var apiUrl = config["services:backgammon-api:http:0"]
              ?? config["services:backgammon-api:https:0"]
              ?? config["ConnectionStrings:backgammon-api"]
              ?? "http://localhost:5000";

    // Debug logging
    Console.WriteLine($"[WebClient] /api/config called");
    Console.WriteLine($"[WebClient] Resolved API URL: {apiUrl}");
    Console.WriteLine($"[WebClient] services:backgammon-api:http:0 = {config["services:backgammon-api:http:0"]}");
    Console.WriteLine($"[WebClient] services:backgammon-api:https:0 = {config["services:backgammon-api:https:0"]}");

    return new { signalrUrl = $"{apiUrl}/gamehub" };
});

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

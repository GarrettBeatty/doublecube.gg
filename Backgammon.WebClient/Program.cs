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
    var apiUrl = config["services:backgammon-api:http:0"] ?? "http://localhost:5000";
    return new { signalrUrl = $"{apiUrl}/gamehub" };
});

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

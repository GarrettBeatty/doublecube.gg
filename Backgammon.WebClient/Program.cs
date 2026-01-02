using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire integration
builder.AddServiceDefaults();

// Configure forwarded headers for reverse proxy support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Map default health endpoints
app.MapDefaultEndpoints();

// Use forwarded headers from Caddy reverse proxy
app.UseForwardedHeaders();

// Serve static files (HTML, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

// Expose API URL to browser via endpoint
app.MapGet("/api/config", (HttpContext context, IConfiguration config) =>
{
    string apiUrl;

    // In production (Docker Compose), use the public URL that the browser accessed
    // In development (Aspire), use service discovery
    var aspireApiUrl = config["services:backgammon-api:http:0"]
                    ?? config["services:backgammon-api:https:0"]
                    ?? config["ConnectionStrings:backgammon-api"];

    if (!string.IsNullOrEmpty(aspireApiUrl))
    {
        // Aspire service discovery - use the discovered URL (development mode)
        apiUrl = aspireApiUrl;
        Console.WriteLine($"[WebClient] Using Aspire service discovery: {apiUrl}");
    }
    else
    {
        // Production mode - browser needs to connect via the same host it used to reach WebClient
        // This works because Caddy reverse proxy routes all traffic through the same public domain
        var publicUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        apiUrl = publicUrl;
        Console.WriteLine($"[WebClient] Using public URL for production: {apiUrl}");
    }

    // Debug logging
    Console.WriteLine($"[WebClient] /api/config called");
    Console.WriteLine($"[WebClient] Resolved SignalR URL: {apiUrl}/gamehub");

    return new { signalrUrl = $"{apiUrl}/gamehub" };
});

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

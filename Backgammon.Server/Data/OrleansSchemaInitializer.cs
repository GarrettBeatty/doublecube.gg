using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Backgammon.Server.Data;

/// <summary>
/// Bootstraps the Orleans AdoNet persistence schema in Postgres on startup.
/// Reads the embedded SQL scripts (downloaded from the Orleans v9.2.1 source tree)
/// and runs them idempotently — CREATE TABLE IF NOT EXISTS and ON CONFLICT DO NOTHING
/// ensure re-runs are safe.
/// </summary>
public static class OrleansSchemaInitializer
{
    private static readonly string[] OrderedScripts =
    {
        "Backgammon.Server.Data.OrleansSchema.PostgreSQL-Main.sql",
        "Backgammon.Server.Data.OrleansSchema.PostgreSQL-Persistence.sql",
    };

    public static async Task EnsureSchemaAsync(string connectionString, ILogger logger, CancellationToken ct = default)
    {
        var assembly = Assembly.GetExecutingAssembly();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var resourceName in OrderedScripts)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded Orleans schema script not found: {resourceName}");

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            logger.LogInformation("Applying Orleans schema script {Script}", resourceName);

            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        logger.LogInformation("Orleans Postgres schema initialized");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backgammon.Server.Data;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Uses a placeholder connection string — migrations only need the provider, not a live database.
/// </summary>
public class BackgammonDbContextFactory : IDesignTimeDbContextFactory<BackgammonDbContext>
{
    /// <inheritdoc />
    public BackgammonDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BackgammonDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=backgammon;Username=backgammon;Password=backgammon");

        return new BackgammonDbContext(optionsBuilder.Options);
    }
}

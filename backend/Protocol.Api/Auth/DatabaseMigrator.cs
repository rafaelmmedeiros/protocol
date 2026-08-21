using Microsoft.EntityFrameworkCore;

namespace Protocol.Api.Auth;

/// <summary>
/// Applies pending migrations at startup. Implemented as a hosted service on purpose: code
/// placed between <c>builder.Build()</c> and <c>app.Run()</c> also executes under
/// <c>dotnet ef</c>, which would make every design-time command require a live database.
/// </summary>
public sealed class DatabaseMigrator(IServiceProvider services, ILogger<DatabaseMigrator> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

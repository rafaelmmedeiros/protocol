using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;

namespace Protocol.Api.Training;

/// <summary>
/// Seeds the exercise catalogue at startup, without a network call.
/// <para>
/// A hosted service for the same reason <see cref="DatabaseMigrator"/> is one: code placed
/// between <c>builder.Build()</c> and <c>app.Run()</c> also executes under <c>dotnet ef</c>,
/// which would make every design-time command require a live database. It must be registered
/// <b>after</b> the migrator, because hosted services start in registration order and there is
/// no table to seed until the migrations have run.
/// </para>
/// <para>
/// Idempotent by <see cref="Exercise.ExternalTemplateId"/>: rows already present are left
/// untouched, so a restart never duplicates the catalogue and never rewrites an identifier a
/// generated week already references.
/// </para>
/// </summary>
public sealed class ExerciseCatalogueSeeder(IServiceProvider services, ILogger<ExerciseCatalogueSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var known = await db.Exercises
            .Select(exercise => exercise.ExternalTemplateId)
            .ToListAsync(cancellationToken);

        var missing = ExerciseCatalogue.All
            .Where(exercise => !known.Contains(exercise.ExternalTemplateId))
            .ToList();

        if (missing.Count == 0)
        {
            logger.LogInformation("Exercise catalogue already seeded ({Count} exercises).", known.Count);
            return;
        }

        db.Exercises.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Exercise catalogue seeded with {Count} exercises.", missing.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

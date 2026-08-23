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

        var existing = await db.Exercises
            .Include(exercise => exercise.Requirements)
            .ToListAsync(cancellationToken);

        var known = existing.Select(exercise => exercise.ExternalTemplateId).ToHashSet();

        var missing = ExerciseCatalogue.All
            .Where(exercise => !known.Contains(exercise.ExternalTemplateId))
            .ToList();

        db.Exercises.AddRange(missing);

        // Backfill: a row seeded before requirements existed has none, and an exercise with no
        // requirements is unperformable under ADR-013 -- so every already-seeded catalogue would
        // generate an empty week. Idempotency by external id means the insert above would never
        // touch them, which is exactly why this has to be here and not in a migration.
        var backfilled = 0;
        foreach (var exercise in existing.Where(exercise => exercise.Requirements.Count == 0))
        {
            var source = ExerciseCatalogue.All
                .SingleOrDefault(row => row.ExternalTemplateId == exercise.ExternalTemplateId);
            if (source is null) continue;

            foreach (var requirement in source.Requirements)
            {
                exercise.Requirements.Add(new ExerciseRequirement { Item = requirement.Item });
            }

            backfilled++;
        }

        if (missing.Count == 0 && backfilled == 0)
        {
            logger.LogInformation("Exercise catalogue already seeded ({Count} exercises).", known.Count);
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Exercise catalogue seeded with {Added} exercises; {Backfilled} had requirements added.",
            missing.Count,
            backfilled);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

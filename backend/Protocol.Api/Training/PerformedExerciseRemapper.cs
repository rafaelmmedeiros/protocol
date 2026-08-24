using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;

namespace Protocol.Api.Training;

/// <summary>
/// Fills in the mapping of imported training that the catalogue can now explain (ADR-026).
/// <para>
/// <see cref="PerformedExercise.ExerciseId"/> is resolved once, at import, from the catalogue as it
/// stood that day. `ADR-018` imports from a cursor, so a workout already read is never fetched
/// again — which means widening the catalogue improves coverage only for training logged *after*
/// the widening. `M4` grew the catalogue from 36 rows to 63 and the coverage number did not move at
/// all: the same 3,798 logged exercises stayed unexplained.
/// </para>
/// <para>
/// <b>This writes to imported rows, and root standard 7 says those are never mutated.</b> ADR-026
/// draws the line it relies on: <see cref="PerformedExercise.ExternalTemplateId"/> and everything in
/// <see cref="PerformedSet"/> are the observation and stay untouchable, while
/// <see cref="PerformedExercise.ExerciseId"/> is <i>our</i> answer to "which of our exercises is
/// that?", derived from data already stored. Recomputing an answer is not mutating an observation.
/// The test for any future write here is whether it could be recomputed without asking Hevy
/// anything — this can; a weight cannot.
/// </para>
/// <para>
/// A hosted service rather than a migration, for the same reason the requirements backfill in
/// <see cref="ExerciseCatalogueSeeder"/> is not one: this is not a one-off. Every catalogue widening
/// creates exactly this gap, and a migration would close today's while leaving the next to be found
/// the same way — by measuring and being surprised. It must be registered <b>after</b> the seeder,
/// because hosted services start in registration order and there is nothing new to map until the
/// new rows exist.
/// </para>
/// </summary>
public sealed class PerformedExerciseRemapper(
    IServiceProvider services,
    ILogger<PerformedExerciseRemapper> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var performed = db.Set<PerformedExercise>();

        // Only unmapped rows are candidates. A row mapped under an earlier catalogue keeps the
        // exercise it was mapped to: a movement whose meaning genuinely changed is a supersession
        // in the catalogue, not a silent remap of somebody's history.
        //
        // The Any() is not redundant with the SetProperty below. Without it every unmapped row is
        // rewritten with null — no change in value, but a write, and a count that would report
        // work it did not do.
        var remapped = await performed
            .Where(exercise => exercise.ExerciseId == null
                && db.Exercises.Any(ours => ours.ExternalTemplateId == exercise.ExternalTemplateId))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    exercise => exercise.ExerciseId,
                    exercise => db.Exercises
                        .Where(ours => ours.ExternalTemplateId == exercise.ExternalTemplateId)
                        .Select(ours => (Guid?)ours.Id)
                        .FirstOrDefault()),
                cancellationToken);

        if (remapped == 0)
        {
            // Not silence: a remap that did nothing and a remap that fixed 3,798 rows look
            // identical from every screen afterwards (root standard 12).
            logger.LogInformation("No imported training needed remapping.");
            return;
        }

        logger.LogInformation(
            "Remapped {Count} imported exercises the catalogue can now explain (ADR-026).",
            remapped);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Imported training the catalogue can now explain gets its mapping filled in (ADR-026).
/// <para>
/// Reads and writes the context directly, and the standing rule in `backend/CLAUDE.md` does not
/// reach this: what is under test is a hosted service that runs at startup with no endpoint in
/// front of it, and the thing asserted is the state of a column that exists precisely so reads do
/// not have to join. There is no API surface that could express it.
/// </para>
/// </summary>
public class PerformedExerciseRemapperTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<Guid> AnImportedWorkoutAsync(string userId, string templateId, Guid? mappedTo)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workout = new PerformedWorkout
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ExternalWorkoutId = $"w-{Guid.NewGuid():N}",
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow,
            ExternallyUpdatedAt = DateTimeOffset.UtcNow,
            Version = 1,
            Exercises =
            [
                new PerformedExercise
                {
                    Position = 0,
                    ExerciseId = mappedTo,
                    ExternalTemplateId = templateId,
                    ExternalTitle = "Whatever it was called",
                    Sets = [new PerformedSet { Position = 0, Kind = SetKind.Working, WeightKg = 40, Reps = 10 }],
                },
            ],
        };

        db.PerformedWorkouts.Add(workout);
        await db.SaveChangesAsync();
        return workout.Id;
    }

    private async Task RunRemapAsync()
    {
        var remapper = new PerformedExerciseRemapper(
            factory.Services,
            NullLogger<PerformedExerciseRemapper>.Instance);

        await remapper.StartAsync(CancellationToken.None);
    }

    private async Task<PerformedExercise> ExerciseOfAsync(Guid workoutId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.PerformedWorkouts
            .AsNoTracking()
            .Where(workout => workout.Id == workoutId)
            .SelectMany(workout => workout.Exercises)
            .SingleAsync();
    }

    [Fact]
    public async Task Training_imported_before_the_catalogue_knew_the_movement_gets_mapped()
    {
        // The real case, and the one M4 measured and missed: 3,798 logged exercises stayed
        // unexplained after the catalogue grew from 36 rows to 63, because ExerciseId is resolved
        // once at import and ADR-018 never re-reads a workout it has already seen.
        var legCurl = await CatalogueRowAsync("11A123F3");   // Seated Leg Curl (Machine), M4
        var workoutId = await AnImportedWorkoutAsync("user-remap-1", "11A123F3", mappedTo: null);

        await RunRemapAsync();

        var exercise = await ExerciseOfAsync(workoutId);
        Assert.Equal(legCurl, exercise.ExerciseId);

        // Theirs, and untouched: the observation is not what was recomputed (ADR-026).
        Assert.Equal("11A123F3", exercise.ExternalTemplateId);
        Assert.Equal("Whatever it was called", exercise.ExternalTitle);
    }

    [Fact]
    public async Task A_movement_the_catalogue_still_does_not_model_stays_unmapped()
    {
        // Null is the honest answer and the one S4.5 reports. A remap that drove this count to
        // zero would be claiming the catalogue models everything, which it does not.
        var workoutId = await AnImportedWorkoutAsync("user-remap-2", "NOT-OURS", mappedTo: null);

        await RunRemapAsync();

        Assert.Null((await ExerciseOfAsync(workoutId)).ExerciseId);
    }

    [Fact]
    public async Task An_existing_mapping_is_never_overwritten()
    {
        // A row mapped under an earlier catalogue keeps the exercise it was mapped to. A movement
        // whose meaning genuinely changed is a supersession in the catalogue, not a silent rewrite
        // of somebody's history.
        var wrong = await CatalogueRowAsync("D04AC939");     // Squat (Barbell): deliberately not the match
        var workoutId = await AnImportedWorkoutAsync("user-remap-3", "11A123F3", mappedTo: wrong);

        await RunRemapAsync();

        Assert.Equal(wrong, (await ExerciseOfAsync(workoutId)).ExerciseId);
    }

    [Fact]
    public async Task Running_it_again_changes_nothing()
    {
        // It runs on every startup, so it has to be free once there is nothing to do.
        var workoutId = await AnImportedWorkoutAsync("user-remap-4", "11A123F3", mappedTo: null);

        await RunRemapAsync();
        var first = (await ExerciseOfAsync(workoutId)).ExerciseId;

        await RunRemapAsync();
        Assert.Equal(first, (await ExerciseOfAsync(workoutId)).ExerciseId);
    }

    private async Task<Guid> CatalogueRowAsync(string templateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.ExternalTemplateId == templateId)
            .Select(exercise => exercise.Id)
            .SingleAsync();
    }
}

using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// Counting what was performed the same way the generator counts what was planned (TD-006).
/// </summary>
public class ImportedVolumeTests
{
    private static Exercise AnExercise() =>
        ExerciseCatalogue.All.First(exercise => exercise.Muscles.Any(m => m.Role == MuscleRole.Secondary));

    private static IReadOnlyDictionary<Guid, Exercise> Catalogue() =>
        ExerciseCatalogue.All.ToDictionary(exercise => exercise.Id);

    private static PerformedWorkout AWorkout(
        Exercise? exercise,
        int version = 1,
        bool deleted = false,
        string? externalId = null,
        params SetKind[] kinds) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = externalId ?? "workout-1",
        StartedAt = DateTimeOffset.UnixEpoch,
        EndedAt = DateTimeOffset.UnixEpoch,
        ExternallyUpdatedAt = DateTimeOffset.UnixEpoch.AddDays(version),
        Version = version,
        IsDeleted = deleted,
        Exercises = deleted
            ? []
            : [
                new PerformedExercise
                {
                    Position = 0,
                    ExerciseId = exercise?.Id,
                    ExternalTemplateId = exercise?.ExternalTemplateId ?? "UNKNOWN",
                    Sets = [.. kinds.Select((kind, index) => new PerformedSet
                    {
                        Position = index,
                        Kind = kind,
                        WeightKg = 50,
                        Reps = 10,
                    })],
                },
            ],
    };

    [Fact]
    public void A_warm_up_set_is_stored_and_never_counted()
    {
        // Retained on import and excluded here, never dropped on the way in (ADR-018). Counting
        // warm-ups would inflate every number this system produces.
        var exercise = AnExercise();
        var workout = AWorkout(exercise, kinds: [SetKind.WarmUp, SetKind.WarmUp, SetKind.Working]);

        // The record of what happened is complete: three sets, two of them warm-ups.
        Assert.Equal(3, workout.Exercises.Single().Sets.Count);

        var volumes = PerformedVolume.ByMuscle([workout], Catalogue());
        var primary = exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup;

        Assert.Equal(1.0m, volumes[primary]);
    }

    [Fact]
    public void An_indirect_muscle_is_credited_half_a_set()
    {
        var exercise = AnExercise();
        var volumes = PerformedVolume.ByMuscle(
            [AWorkout(exercise, kinds: [SetKind.Working, SetKind.Working])],
            Catalogue());

        var primary = exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup;
        var secondary = exercise.Muscles.First(m => m.Role == MuscleRole.Secondary).MuscleGroup;

        Assert.Equal(2.0m, volumes[primary]);      // TD-006
        Assert.Equal(1.0m, volumes[secondary]);    // two sets at 0.5 each
    }

    [Fact]
    public void Drop_sets_and_failure_sets_are_not_counted_as_working_sets()
    {
        // Neither is ever prescribed by this system (TD-013, TD-018), so crediting them would
        // count volume against a prescription that did not ask for it.
        var exercise = AnExercise();
        var volumes = PerformedVolume.ByMuscle(
            [AWorkout(exercise, kinds: [SetKind.Working, SetKind.DropSet, SetKind.ToFailure])],
            Catalogue());

        var primary = exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup;
        Assert.Equal(1.0m, volumes[primary]);
    }

    [Fact]
    public void An_exercise_outside_our_catalogue_credits_nothing()
    {
        // We do not know what it loads. A gap in the catalogue rather than in the training, and
        // ADR-020 is what surfaces it instead of letting it read as rest.
        var volumes = PerformedVolume.ByMuscle(
            [AWorkout(exercise: null, kinds: [SetKind.Working, SetKind.Working])],
            Catalogue());

        Assert.Empty(volumes);
    }

    [Fact]
    public void The_current_reading_is_the_highest_version()
    {
        var exercise = AnExercise();

        var current = PerformedVolume.Current([
            AWorkout(exercise, version: 1, kinds: [SetKind.Working, SetKind.Working, SetKind.Working]),
            AWorkout(exercise, version: 2, kinds: [SetKind.Working]),
        ]);

        Assert.Equal(2, Assert.Single(current).Version);
        Assert.Equal(
            1.0m,
            PerformedVolume.ByMuscle(current, Catalogue())[
                exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup]);
    }

    [Fact]
    public void A_tombstoned_workout_stops_counting_without_any_row_being_removed()
    {
        // The acceptance criterion, and the shape of root standard 7: the earlier version is
        // still there and still readable -- it simply is not the reading that counts.
        var exercise = AnExercise();

        var versions = new[]
        {
            AWorkout(exercise, version: 1, kinds: [SetKind.Working, SetKind.Working]),
            AWorkout(exercise, version: 2, deleted: true),
        };

        Assert.Empty(PerformedVolume.Current(versions));
        Assert.Empty(PerformedVolume.ByMuscle(PerformedVolume.Current(versions), Catalogue()));

        // Nothing was removed to achieve that.
        Assert.Equal(2, versions.Length);
        Assert.Equal(2, versions[0].Exercises.Single().Sets.Count);
    }

    [Fact]
    public void Unbound_history_still_counts_toward_volume()
    {
        // ADR-019 takes the narrow join and lets a workout that matched no session stay unbound.
        // That degrades honestly only if the training still counts where progression actually
        // reads it -- at the exercise, not at the session.
        var exercise = AnExercise();

        var freestyle = AWorkout(exercise, externalId: "walk-in", kinds: [SetKind.Working, SetKind.Working]);
        Assert.Null(freestyle.ExternalRoutineId);

        var volumes = PerformedVolume.ByMuscle(PerformedVolume.Current([freestyle]), Catalogue());
        var primary = exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup;

        Assert.Equal(2.0m, volumes[primary]);
    }

    [Fact]
    public void Different_workouts_are_counted_separately()
    {
        var exercise = AnExercise();

        var current = PerformedVolume.Current([
            AWorkout(exercise, externalId: "a", kinds: [SetKind.Working]),
            AWorkout(exercise, externalId: "b", kinds: [SetKind.Working]),
        ]);

        Assert.Equal(2, current.Count);
        Assert.Equal(
            2.0m,
            PerformedVolume.ByMuscle(current, Catalogue())[
                exercise.Muscles.First(m => m.Role == MuscleRole.Primary).MuscleGroup]);
    }

    // ---- Volume-load (S4.4, ADR-024) ---------------------------------------------------------

    private static PerformedWorkout ALoggedLift(Exercise exercise, params (SetKind Kind, double? Weight, double? Reps)[] sets) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = "workout-load",
        StartedAt = DateTimeOffset.UnixEpoch,
        EndedAt = DateTimeOffset.UnixEpoch,
        ExternallyUpdatedAt = DateTimeOffset.UnixEpoch,
        Version = 1,
        Exercises =
        [
            new PerformedExercise
            {
                Position = 0,
                ExerciseId = exercise.Id,
                ExternalTemplateId = exercise.ExternalTemplateId,
                Sets = [.. sets.Select((set, index) => new PerformedSet
                {
                    Position = index,
                    Kind = set.Kind,
                    WeightKg = set.Weight,
                    Reps = set.Reps,
                })],
            },
        ],
    };

    private static Exercise ByTemplate(string id) =>
        ExerciseCatalogue.All.Single(exercise => exercise.ExternalTemplateId == id);

    [Fact]
    public void Volume_load_counts_working_sets_and_ignores_every_other_kind()
    {
        // The same exclusion ByMuscle applies, for the same reason: a warm-up is retained on
        // import (ADR-018) and is not training this system asked for. Counting it would inflate
        // every kilogram figure M5 ever compares against.
        var exercise = ByTemplate("A5AC6449");   // Bicep Curl (Barbell)

        var workout = ALoggedLift(
            exercise,
            (SetKind.WarmUp, 20, 10),
            (SetKind.Working, 30, 10),
            (SetKind.DropSet, 20, 12),
            (SetKind.ToFailure, 30, 6));

        Assert.Equal(300m, PerformedVolume.VolumeLoadOf(workout.Exercises.Single()));
    }

    [Fact]
    public void A_barbell_and_a_dumbbell_set_at_the_same_weight_count_the_same()
    {
        // ADR-024: the log is a total, whatever the implement. The engineer's own history says so
        // — dumbbell curls sit alongside barbell curls at 18.9 against 18.8 kg across 233 sets,
        // where a per-hand reading would put them at half.
        //
        // This is the guard against the tempting fix: inspecting Equipment and doubling the
        // dumbbell figure. That is option B of the record, and it would apply a factor-of-two
        // error to every dumbbell movement the system reasons about.
        var barbell = ByTemplate("A5AC6449");    // Bicep Curl (Barbell)
        var dumbbell = ByTemplate("37FCC2BB");   // Bicep Curl (Dumbbell)

        Assert.Equal(Equipment.Barbell, barbell.Equipment);
        Assert.Equal(Equipment.Dumbbell, dumbbell.Equipment);

        var withBar = ALoggedLift(barbell, (SetKind.Working, 30, 10));
        var withDumbbells = ALoggedLift(dumbbell, (SetKind.Working, 30, 10));

        Assert.Equal(
            PerformedVolume.VolumeLoadOf(withBar.Exercises.Single()),
            PerformedVolume.VolumeLoadOf(withDumbbells.Exercises.Single()));

        // And the same at the muscle they share, where the fractional credit is applied.
        var barLoads = PerformedVolume.VolumeLoadByMuscle([withBar], Catalogue());
        var dumbbellLoads = PerformedVolume.VolumeLoadByMuscle([withDumbbells], Catalogue());

        Assert.Equal(barLoads[MuscleGroup.Biceps], dumbbellLoads[MuscleGroup.Biceps]);
    }

    [Fact]
    public void An_indirect_muscle_is_credited_half_the_kilograms()
    {
        // The same TD-006 rule the set count uses. Planned and performed have to be counted the
        // same way, and so do sets and kilograms — two quantities counted on different rules
        // cannot be read side by side.
        var exercise = ByTemplate("A5AC6449");   // Bicep Curl (Barbell): biceps primary, forearms secondary
        var workout = ALoggedLift(exercise, (SetKind.Working, 30, 10));

        var loads = PerformedVolume.VolumeLoadByMuscle([workout], Catalogue());

        Assert.Equal(300m, loads[MuscleGroup.Biceps]);
        Assert.Equal(150m, loads[MuscleGroup.Forearms]);
    }

    [Fact]
    public void A_set_with_no_load_contributes_no_kilograms_and_is_still_a_set()
    {
        // Bodyweight work stores a null load because the load is the body and Hevy does not report
        // it. Reading that as zero kilograms would be a claim rather than an absence — but the set
        // still happened, and the set count still credits it.
        var exercise = ByTemplate("1B2B1E7C");   // Pull Up
        var workout = ALoggedLift(exercise, (SetKind.Working, null, 8));

        Assert.Empty(PerformedVolume.VolumeLoadByMuscle([workout], Catalogue()));
        Assert.NotEmpty(PerformedVolume.ByMuscle([workout], Catalogue()));
    }
}

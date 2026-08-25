using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// What a muscle has accumulated, and the two ways it can be short. The distinction is the point:
/// deferred volume arrives when the session ahead of it does, and skipped volume never does
/// (`ADR-032`). A report that added them together would be wrong in the flattering direction.
/// </summary>
public class PerformedVolumeTests
{
    private static readonly Exercise Squat =
        ExerciseCatalogue.All.First(e => e.Muscles.Any(m => m.Role == MuscleRole.Secondary));

    private static MuscleGroup PrimaryOf(Exercise exercise) =>
        exercise.Muscles.Single(muscle => muscle.Role == MuscleRole.Primary).MuscleGroup;

    private static IReadOnlyDictionary<Guid, Exercise> Catalogue() =>
        ExerciseCatalogue.All.ToDictionary(exercise => exercise.Id);

    private static GeneratedWeek APlan(params (SessionDeclaration? Declared, int Sets)[] sessions) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        GeneratedAt = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero),
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = 2,
        SessionDurationSeconds = 3_600,
        WeeklyTargetFractionalSets = 6.0m,
        WeeklyCeilingFractionalSets = 8.0m,
        Sessions =
        [
            .. sessions.Select((session, index) => new GeneratedSession
            {
                Id = Guid.CreateVersion7(),
                Position = index + 1,
                Kind = SessionKind.FullBody,
                Declared = session.Declared,
                Prescriptions =
                [
                    new GeneratedPrescription
                    {
                        Id = Guid.CreateVersion7(),
                        Position = 1,
                        ExerciseId = Squat.Id,
                        Sets = session.Sets,
                        MinReps = 6,
                        MaxReps = 10,
                        RepsInReserve = 2,
                        RestSeconds = 180,
                    },
                ],
            }),
        ],
    };

    private static PerformedWorkout AWorkout(DateTimeOffset startedAt, int working, int warmUp = 0) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = Guid.CreateVersion7().ToString(),
        StartedAt = startedAt,
        EndedAt = startedAt.AddHours(1),
        ExternallyUpdatedAt = startedAt,
        Version = 0,
        Exercises =
        [
            new PerformedExercise
            {
                Id = Guid.CreateVersion7(),
                Position = 0,
                ExternalTemplateId = Squat.ExternalTemplateId,
                ExerciseId = Squat.Id,
                Sets =
                [
                    .. Enumerable.Range(0, working).Select(i => new PerformedSet
                    {
                        Id = Guid.CreateVersion7(),
                        Position = i,
                        Kind = SetKind.Working,
                    }),
                    .. Enumerable.Range(0, warmUp).Select(i => new PerformedSet
                    {
                        Id = Guid.CreateVersion7(),
                        Position = working + i,
                        Kind = SetKind.WarmUp,
                    }),
                ],
            },
        ],
    };

    [Fact]
    public void Performed_volume_accumulates_across_weeks_and_ignores_warm_up_sets()
    {
        var monday = new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero);

        var report = TrainingAccumulation.Build(
            [],
            null,
            [
                AWorkout(monday, working: 3, warmUp: 4),
                AWorkout(monday.AddDays(7), working: 2, warmUp: 9),
            ],
            Catalogue());

        var primary = report.Muscles.Single(entry => entry.MuscleGroup == PrimaryOf(Squat).ToString());

        // Five working sets across two Monday-anchored weeks. The thirteen warm-up sets credit
        // nothing: they are retained on import and excluded from the arithmetic (TD-006).
        Assert.Equal(5.0m, primary.Performed);
        Assert.Equal(2, report.WeeksMeasured);
    }

    [Fact]
    public void A_skipped_session_is_reported_apart_from_one_still_ahead_in_the_queue()
    {
        var plan = APlan(
            (SessionDeclaration.Skipped, 3),
            (null, 3));

        var report = TrainingAccumulation.Build([plan], plan, [], Catalogue());
        var primary = report.Muscles.Single(entry => entry.MuscleGroup == PrimaryOf(Squat).ToString());

        // Same three sets either way, and opposite meanings: one arrives when the session does,
        // the other never arrives at all (ADR-032).
        Assert.Equal(3.0m, primary.Skipped);
        Assert.Equal(3.0m, primary.Deferred);
        Assert.Equal(0m, primary.Performed);
    }

    [Fact]
    public void Skipping_the_same_session_across_cycles_grows_the_number()
    {
        // The acceptance criterion, as arithmetic: four cycles, the same session skipped in each.
        var plans = Enumerable.Range(0, 4).Select(_ => APlan((SessionDeclaration.Skipped, 3))).ToList();

        var report = TrainingAccumulation.Build(plans, plans[0], [], Catalogue());
        var primary = report.Muscles.Single(entry => entry.MuscleGroup == PrimaryOf(Squat).ToString());

        Assert.Equal(12.0m, primary.Skipped);
    }

    [Fact]
    public void A_superseded_plans_untouched_sessions_are_not_skips()
    {
        // Regenerating writes a new week and leaves the old one standing (ADR-009). Its sessions
        // were never declared anything, and counting them as lost volume would make pressing the
        // button a deficit.
        var superseded = APlan((null, 3), (null, 3));
        var current = APlan((null, 3));

        var report = TrainingAccumulation.Build([current, superseded], current, [], Catalogue());
        var primary = report.Muscles.Single(entry => entry.MuscleGroup == PrimaryOf(Squat).ToString());

        Assert.Equal(0m, primary.Skipped);
        Assert.Equal(3.0m, primary.Deferred);
    }

    [Fact]
    public void The_target_comes_from_the_plan_rather_than_from_todays_constant()
    {
        var plan = APlan((null, 3));

        Assert.Equal(6.0m, TrainingAccumulation.Build([plan], plan, [], Catalogue()).TargetPerCycle);
    }
}

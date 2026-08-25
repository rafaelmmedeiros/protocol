using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// The binding rate, which `ADR-019` names as the evidence that would justify revisiting how a
/// workout matches a session. A number reported against the wrong denominator is worse than no
/// number: it reads as a broken join when the join never had a chance to fire.
/// </summary>
public class WeekComparisonTests
{
    private static readonly DateTimeOffset Generated = new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

    private static GeneratedWeek AWeek(string? routineId) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        GeneratedAt = Generated,
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = 2,
        SessionDurationSeconds = 3_600,
        WeeklyTargetFractionalSets = TrainingPrescription.WeeklyTargetFractionalSets,
        WeeklyCeilingFractionalSets = TrainingPrescription.WeeklyCeilingFractionalSets,
        Sessions =
        [
            new GeneratedSession
            {
                Id = Guid.CreateVersion7(),
                Position = 1,
                Kind = SessionKind.FullBody,
                HevyRoutineId = routineId,
                Prescriptions = [],
            },
        ],
    };

    private static PerformedWorkout AWorkout(DateTimeOffset startedAt, string? routineId) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = Guid.CreateVersion7().ToString(),
        ExternalRoutineId = routineId,
        StartedAt = startedAt,
        EndedAt = startedAt.AddHours(1),
        ExternallyUpdatedAt = startedAt,
        Version = 0,
        Exercises = [],
    };

    [Fact]
    public void History_that_predates_the_plan_is_not_counted_against_the_binding_rate()
    {
        // The defect ADR-019 records: a real account held 759 imported workouts and 1 bound, and
        // the 758 were logged before any routine existed to bind to. Counting them made a join
        // that worked perfectly read as 0.1%.
        var week = AWeek("routine-1");

        var comparison = WeekComparisonBuilder.Build(
            week,
            [
                AWorkout(Generated.AddDays(-30), null),
                AWorkout(Generated.AddDays(-10), null),
                AWorkout(Generated.AddDays(1), "routine-1"),
            ]);

        Assert.Equal(1, comparison.Coverage.ImportedWorkouts);
        Assert.Equal(1, comparison.Coverage.BoundWorkouts);
    }

    [Fact]
    public void Training_since_the_plan_that_bound_to_nothing_still_counts_against_it()
    {
        // The other direction, and the one that keeps the number honest: a workout logged since
        // this plan and matched to none of its sessions is exactly what a low rate means.
        var week = AWeek("routine-1");

        var comparison = WeekComparisonBuilder.Build(
            week,
            [
                AWorkout(Generated.AddDays(1), null),
                AWorkout(Generated.AddDays(2), "someone-elses-routine"),
            ]);

        Assert.Equal(2, comparison.Coverage.ImportedWorkouts);
        Assert.Equal(0, comparison.Coverage.BoundWorkouts);
        Assert.Equal(2, comparison.UnboundWorkouts.Count);
    }

    [Fact]
    public void A_plan_that_was_never_pushed_reports_no_bindings_rather_than_a_rate_of_zero()
    {
        // No routine id means nothing could bind, which is not the same claim as "nothing did".
        var week = AWeek(routineId: null);

        var comparison = WeekComparisonBuilder.Build(week, [AWorkout(Generated.AddDays(1), "routine-1")]);

        Assert.Equal(0, comparison.Coverage.BoundWorkouts);
        Assert.Single(comparison.UnboundWorkouts);
    }
}

using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// A logged workout binds to the session that prescribed it by identifier alone (ADR-019).
/// </summary>
public class WorkoutBindingTests
{
    private const string RoutineId = "7b2281f1-0000-4000-8000-000000000001";

    private static Exercise AnExercise() => ExerciseCatalogue.All.First();

    private static GeneratedWeek AWeek(string? routineId = RoutineId, int minReps = 8, int maxReps = 12)
    {
        var exercise = AnExercise();

        return new GeneratedWeek
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            WeekStartDate = new DateOnly(2026, 8, 24),
            GeneratedAt = DateTimeOffset.UnixEpoch,
            Goal = TrainingGoal.Hypertrophy,
            DaysPerWeek = 3,
            SessionDurationSeconds = 5_400,
            WeeklyTargetFractionalSets = TrainingPrescription.WeeklyTargetFractionalSets,   // TD-014
            WeeklyCeilingFractionalSets = TrainingPrescription.WeeklyCeilingFractionalSets, // TD-022
            Sessions =
            [
                new GeneratedSession
                {
                    Position = 1,
                    Day = DayOfWeek.Monday,
                    Kind = SessionKind.FullBody,
                    HevyRoutineId = routineId,
                    Prescriptions =
                    [
                        new GeneratedPrescription
                        {
                            Id = Guid.CreateVersion7(),
                            Position = 1,
                            ExerciseId = exercise.Id,
                            Exercise = exercise,
                            Sets = 3,
                            MinReps = minReps,
                            MaxReps = maxReps,
                            RepsInReserve = 2,
                            RestSeconds = 150,
                        },
                    ],
                },
            ],
        };
    }

    private static PerformedWorkout AWorkout(
        string? routineId,
        string? title = "Full body",
        Guid? exerciseId = null,
        int? reserve = null,
        params double[] reps) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = $"w-{Guid.NewGuid():N}",
        ExternalRoutineId = routineId,
        ExternalTitle = title,
        StartedAt = new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero),
        EndedAt = new DateTimeOffset(2026, 8, 24, 16, 0, 0, TimeSpan.Zero),
        ExternallyUpdatedAt = new DateTimeOffset(2026, 8, 24, 16, 5, 0, TimeSpan.Zero),
        Version = 1,
        Exercises =
        [
            new PerformedExercise
            {
                Position = 0,
                ExerciseId = exerciseId ?? AnExercise().Id,
                ExternalTemplateId = AnExercise().ExternalTemplateId,
                ExternalTitle = "Whatever Hevy calls it",
                Sets = [.. reps.Select((count, index) => new PerformedSet
                {
                    Position = index,
                    Kind = SetKind.Working,
                    WeightKg = 50,
                    Reps = count,
                    RepsInReserve = reserve,
                })],
            },
        ],
    };

    [Fact]
    public void A_workout_started_from_a_pushed_routine_binds_to_that_session()
    {
        var comparison = WeekComparisonBuilder.Build(AWeek(), [AWorkout(RoutineId, reps: [11, 9, 8])]);

        var session = Assert.Single(comparison.Sessions);
        Assert.True(session.Performed);
        Assert.Equal(3, Assert.Single(session.Slots).PerformedSets.Count);
    }

    [Fact]
    public void A_workout_with_a_matching_title_and_no_routine_does_not_bind()
    {
        // Standard 9: a title is display only. Binding on one would break the day the app speaks
        // pt-BR, and would re-bind history whenever a user renamed a routine.
        var week = AWeek();
        var sameTitle = week.Sessions.Single().Kind.ToString();

        var comparison = WeekComparisonBuilder.Build(week, [AWorkout(null, title: sameTitle, reps: [11, 9, 8])]);

        var session = Assert.Single(comparison.Sessions);
        Assert.False(session.Performed);
        Assert.Single(comparison.UnboundWorkouts);
    }

    [Fact]
    public void A_workout_from_a_routine_we_did_not_create_does_not_bind()
    {
        var comparison = WeekComparisonBuilder.Build(
            AWeek(), [AWorkout("some-other-routine", reps: [11])]);

        Assert.False(Assert.Single(comparison.Sessions).Performed);
        Assert.Single(comparison.UnboundWorkouts);
    }

    [Fact]
    public void A_session_that_was_never_pushed_binds_to_nothing()
    {
        var comparison = WeekComparisonBuilder.Build(
            AWeek(routineId: null), [AWorkout(null, reps: [11])]);

        Assert.False(Assert.Single(comparison.Sessions).Performed);
    }

    [Fact]
    public void Unbound_history_is_reported_rather_than_discarded()
    {
        // ADR-019 makes it first-class: it still counts toward volume and toward equipment
        // inference, which is where progression reads anyway.
        var comparison = WeekComparisonBuilder.Build(
            AWeek(), [AWorkout(null, reps: [12]), AWorkout(null, reps: [10])]);

        Assert.Equal(2, comparison.UnboundWorkouts.Count);
        Assert.All(comparison.UnboundWorkouts, workout => Assert.Equal(1, workout.ExerciseCount));
    }

    [Fact]
    public void The_binding_rate_is_reported()
    {
        // The evidence ADR-019 named as what would justify revisiting the narrow join.
        var comparison = WeekComparisonBuilder.Build(
            AWeek(), [AWorkout(RoutineId, reps: [11]), AWorkout(null, reps: [10])]);

        Assert.Equal(2, comparison.Coverage.ImportedWorkouts);
        Assert.Equal(1, comparison.Coverage.BoundWorkouts);
    }

    [Fact]
    public void The_performed_sequence_keeps_its_order()
    {
        // 11/9/8 and 8/9/11 are different facts, and the read model must not be able to confuse
        // them. The sets are handed over shuffled on purpose.
        var workout = AWorkout(RoutineId, reps: [11, 9, 8]);
        var shuffled = workout.Exercises.Single().Sets.Reverse().ToList();
        workout.Exercises.Single().Sets.Clear();
        foreach (var set in shuffled)
        {
            workout.Exercises.Single().Sets.Add(set);
        }

        var slot = Assert.Single(Assert.Single(
            WeekComparisonBuilder.Build(AWeek(), [workout]).Sessions).Slots);

        Assert.Equal([11d, 9d, 8d], slot.PerformedSets.Select(set => set.Reps));
    }

    [Fact]
    public void A_set_with_no_reported_effort_carries_no_reserve()
    {
        // The screen has to tell "reported nothing" from "had nothing left" (TD-017). In every
        // workout read from a real account so far, this is every set.
        var slot = Assert.Single(Assert.Single(
            WeekComparisonBuilder.Build(AWeek(), [AWorkout(RoutineId, reps: [11, 9])]).Sessions).Slots);

        Assert.All(slot.PerformedSets, set => Assert.Null(set.RepsInReserve));
    }

    [Fact]
    public void A_reported_effort_reaches_the_read_model()
    {
        var slot = Assert.Single(Assert.Single(
            WeekComparisonBuilder.Build(AWeek(), [AWorkout(RoutineId, reserve: 1, reps: [11])]).Sessions).Slots);

        Assert.Equal(1, Assert.Single(slot.PerformedSets).RepsInReserve);
    }

    [Theory]
    [InlineData(SlotOutcomes.InRange, 11, 9, 8)]
    [InlineData(SlotOutcomes.AboveRange, 13, 12, 11)]
    [InlineData(SlotOutcomes.BelowRange, 9, 8, 7)]
    [InlineData(SlotOutcomes.Mixed, 13, 9, 7)]
    public void The_outcome_says_where_the_repetitions_landed_and_nothing_more(
        string expected,
        double first,
        double second,
        double third)
    {
        // Deliberately not a judgement about progress: what a sequence means is a training
        // decision M4 makes with a record behind it, and this read model must not pre-empt it.
        var slot = Assert.Single(Assert.Single(
            WeekComparisonBuilder.Build(AWeek(), [AWorkout(RoutineId, reps: [first, second, third])])
                .Sessions).Slots);

        Assert.Equal(expected, slot.Outcome);
    }

    [Fact]
    public void A_slot_nothing_was_done_in_reads_as_not_performed()
    {
        var comparison = WeekComparisonBuilder.Build(AWeek(), []);

        Assert.Equal(SlotOutcomes.NotPerformed, Assert.Single(Assert.Single(comparison.Sessions).Slots).Outcome);
    }

    [Fact]
    public void An_exercise_the_session_did_not_prescribe_is_carried_as_an_extra()
    {
        // Training that happened. Hiding it would make the screen a claim about the plan rather
        // than a record of the day.
        var other = ExerciseCatalogue.All.Skip(1).First();
        var comparison = WeekComparisonBuilder.Build(
            AWeek(), [AWorkout(RoutineId, exerciseId: other.Id, reps: [12])]);

        var session = Assert.Single(comparison.Sessions);

        Assert.Equal(SlotOutcomes.NotPerformed, Assert.Single(session.Slots).Outcome);
        Assert.Single(session.Extras);
    }

    [Fact]
    public void A_workout_deleted_upstream_reads_as_not_performed()
    {
        var week = AWeek();
        var first = AWorkout(RoutineId, reps: [11, 9, 8]);

        var tombstone = new PerformedWorkout
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            ExternalWorkoutId = first.ExternalWorkoutId,
            ExternalRoutineId = RoutineId,
            StartedAt = first.StartedAt,
            EndedAt = first.EndedAt,
            ExternallyUpdatedAt = first.ExternallyUpdatedAt.AddDays(1),
            Version = 2,
            IsDeleted = true,
        };

        var comparison = WeekComparisonBuilder.Build(week, [first, tombstone]);

        Assert.False(Assert.Single(comparison.Sessions).Performed);
        Assert.Equal(0, comparison.Coverage.ImportedWorkouts);
    }

    [Fact]
    public void Warm_up_sets_do_not_appear_in_the_comparison()
    {
        // They are retained on import (ADR-018) and excluded where the comparison is made, for
        // the same reason they are excluded from volume (TD-006).
        var workout = AWorkout(RoutineId, reps: [11, 9]);
        workout.Exercises.Single().Sets.Add(new PerformedSet
        {
            Position = 99,
            Kind = SetKind.WarmUp,
            WeightKg = 20,
            Reps = 12,
        });

        var slot = Assert.Single(Assert.Single(
            WeekComparisonBuilder.Build(AWeek(), [workout]).Sessions).Slots);

        Assert.Equal([11d, 9d], slot.PerformedSets.Select(set => set.Reps));
    }
}

using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// What a pushed routine may and may not carry (ADR-016).
/// <para>
/// <c>HevyMappingTests</c> covers the mapper as a translation. This covers the payload as a
/// promise: the fields that must be absent, and the ones whose value comes from a record rather
/// than from a preference.
/// </para>
/// </summary>
public class RoutinePayloadTests
{
    private static Exercise AnExercise() => ExerciseCatalogue.All.First();

    private static GeneratedPrescription APrescription(
        int position = 1,
        int sets = 3,
        int minReps = 8,
        int maxReps = 12,
        int rest = 150) => new()
    {
        Position = position,
        ExerciseId = AnExercise().Id,
        Exercise = AnExercise(),
        Sets = sets,
        MinReps = minReps,
        MaxReps = maxReps,
        RepsInReserve = 2,
        RestSeconds = rest,
    };

    private static HevyRoutinePayload Routine(params GeneratedPrescription[] prescriptions) =>
        HevyOutboundMapper.ToRoutine("Push", 7, prescriptions, prescription =>
            RoutineNotes.For(prescription, "en-US"));

    [Fact]
    public void No_pushed_set_carries_a_weight()
    {
        // The acceptance criterion, and the reason: M3 prescribes no load, the user chooses it in
        // the gym, and that choice is the observation M4 needs (TD-001, ADR-016). Null rather
        // than zero -- zero is a claim, absence is not.
        var routine = Routine(APrescription(), APrescription(position: 2));

        Assert.All(
            routine.Exercises.SelectMany(exercise => exercise.Sets),
            set => Assert.Null(set.WeightKg));
    }

    [Fact]
    public void No_warm_up_set_is_ever_pushed()
    {
        // TD-012 budgets time for a ramp without prescribing it as sets, and the import filters
        // warmup on the way back (TD-006). Sending none keeps the two directions symmetric.
        var routine = Routine(APrescription());

        Assert.All(
            routine.Exercises.SelectMany(exercise => exercise.Sets),
            set => Assert.Equal("normal", set.Type));
    }

    [Fact]
    public void The_set_count_is_the_one_the_record_decided()
    {
        var routine = Routine(APrescription(sets: 4));

        Assert.Equal(4, Assert.Single(routine.Exercises).Sets.Count);
    }

    [Fact]
    public void A_set_carries_a_range_and_never_a_single_repetition_target()
    {
        // The intervention that costs nothing (ADR-016): a fixed number censors the observation,
        // a range terminated on effort does not. Asserting Reps is null is asserting that.
        var routine = Routine(APrescription(minReps: 6, maxReps: 10));

        Assert.All(Assert.Single(routine.Exercises).Sets, set =>
        {
            Assert.Null(set.Reps);
            Assert.NotNull(set.RepRange);
            Assert.Equal(6, set.RepRange.Start);
            Assert.Equal(10, set.RepRange.End);
        });
    }

    [Fact]
    public void The_note_is_on_the_exercise_and_never_on_a_set()
    {
        // A routine set has no field for prose, and a note repeated per set would be noise in the
        // one place the user reads while training.
        var routine = Routine(APrescription());
        var exercise = Assert.Single(routine.Exercises);

        Assert.False(string.IsNullOrWhiteSpace(exercise.Notes));
    }

    [Fact]
    public void Rest_comes_from_the_record_and_lives_on_the_exercise()
    {
        var routine = Routine(APrescription(rest: 180));

        Assert.Equal(180, Assert.Single(routine.Exercises).RestSeconds);   // TD-011
    }

    [Fact]
    public void The_folder_travels_with_the_routine()
    {
        // ADR-015: one folder per week, so every routine of that week names it.
        Assert.Equal(7, Routine(APrescription()).FolderId);
    }

    [Fact]
    public void Nothing_in_the_payload_carries_an_effort_value()
    {
        // ADR-016's rule, asserted structurally. A routine set has no rpe field, because effort
        // is feedback and a plan does not carry an observation -- and if one ever appears, this
        // test is what stops it being filled in without reading that record.
        var routine = Routine(APrescription());

        Assert.DoesNotContain(
            typeof(HevyRoutineSet).GetProperties(),
            property => property.Name.Contains("rpe", StringComparison.OrdinalIgnoreCase));
    }
}

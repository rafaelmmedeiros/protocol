using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// The two mappers, one per direction (root standard 17).
/// </summary>
public class HevyMappingTests
{
    private static Exercise AnExercise(int skip = 0) => ExerciseCatalogue.All.Skip(skip).First();

    private static GeneratedPrescription APrescription(Exercise exercise, int position = 1) => new()
    {
        Position = position,
        ExerciseId = exercise.Id,
        Exercise = exercise,
        Sets = 3,
        MinReps = 8,
        MaxReps = 12,
        RepsInReserve = 2,
        RestSeconds = 150,
    };

    // -----------------------------------------------------------------------------------------
    // Outbound
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void A_session_becomes_a_routine_of_its_slots_in_order()
    {
        var first = AnExercise();
        var second = AnExercise(1);

        var routine = HevyOutboundMapper.ToRoutine(
            "Push · W1",
            folderId: 42,
            [APrescription(second, position: 2), APrescription(first, position: 1)],
            noteFor: _ => null);

        Assert.Equal("Push · W1", routine.Title);
        Assert.Equal(42, routine.FolderId);
        Assert.Equal(
            [first.ExternalTemplateId, second.ExternalTemplateId],
            routine.Exercises.Select(exercise => exercise.ExerciseTemplateId));
    }

    [Fact]
    public void A_pushed_set_carries_the_range_and_the_rest_and_no_load()
    {
        var routine = HevyOutboundMapper.ToRoutine(
            "Push", null, [APrescription(AnExercise())], noteFor: _ => null);

        var exercise = Assert.Single(routine.Exercises);

        Assert.Equal(150, exercise.RestSeconds);          // TD-011
        Assert.Null(exercise.SupersetId);                 // TD-013 declined supersets
        Assert.Equal(3, exercise.Sets.Count);

        Assert.All(exercise.Sets, set =>
        {
            Assert.Equal("normal", set.Type);             // no warm-up sets are pushed
            Assert.Null(set.WeightKg);                    // M4 fixed what a load means (ADR-024); M6 decides what to ask for
            Assert.Null(set.Reps);                        // a single number is what censors the log
            Assert.NotNull(set.RepRange);
            Assert.Equal(8, set.RepRange.Start);          // TD-009
            Assert.Equal(12, set.RepRange.End);
        });
    }

    [Fact]
    public void The_prescribed_effort_travels_as_a_note_because_a_routine_set_has_no_field_for_it()
    {
        // ADR-016: rpe is feedback, produced after a set, and a plan does not carry an
        // observation. The note is the only channel, and it is display rather than data -- the
        // experiment proved routine notes do not survive into the workout.
        var routine = HevyOutboundMapper.ToRoutine(
            "Push",
            null,
            [APrescription(AnExercise())],
            noteFor: prescription => $"{prescription.MinReps}-{prescription.MaxReps}, "
                + $"about {prescription.RepsInReserve} left");

        var exercise = Assert.Single(routine.Exercises);
        Assert.Equal("8-12, about 2 left", exercise.Notes);
    }

    [Fact]
    public void An_exercise_with_no_external_key_cannot_be_pushed()
    {
        // Loud rather than substituted: putting a neighbouring movement into the user's gym is a
        // choice no record made.
        var orphan = new GeneratedPrescription
        {
            Position = 1,
            ExerciseId = Guid.NewGuid(),
            Exercise = null,
            Sets = 3,
            MinReps = 8,
            MaxReps = 12,
            RepsInReserve = 2,
            RestSeconds = 150,
        };

        Assert.Throws<HevyMappingException>(() =>
            HevyOutboundMapper.ToRoutine("Push", null, [orphan], noteFor: _ => null));
    }

    // -----------------------------------------------------------------------------------------
    // Inbound
    // -----------------------------------------------------------------------------------------

    private static HevyWorkout AWorkout(params HevyWorkoutSet[] sets) => new(
        Id: "65312e26-0000-4000-8000-000000000000",
        Title: "Push",
        RoutineId: "7b2281f1-0000-4000-8000-000000000000",
        Description: null,
        StartTime: new DateTimeOffset(2026, 8, 20, 14, 51, 0, TimeSpan.Zero),
        EndTime: new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero),
        UpdatedAt: new DateTimeOffset(2026, 8, 21, 23, 11, 0, TimeSpan.Zero),
        CreatedAt: new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero),
        Exercises:
        [
            new HevyWorkoutExercise(0, "Bench Press (Barbell)", "", "79D0BB3A", null, sets),
        ]);

    private static HevyWorkoutSet ASet(int index, string type, double? weight, double? reps, double? rpe = null) =>
        new(index, type, weight, reps, null, null, rpe, null);

    [Fact]
    public void A_logged_workout_keeps_its_routine_link_and_its_set_order()
    {
        // The sequence is ordered and is never reduced to a total: 11/9/8 and 8/9/11 are
        // different facts about the same session.
        var workout = AWorkout(
            ASet(2, "normal", 50, 8),
            ASet(0, "normal", 50, 11),
            ASet(1, "normal", 50, 9));

        var performed = HevyInboundMapper.ToPerformedWorkout(workout, "user-1", _ => null);

        Assert.Equal("65312e26-0000-4000-8000-000000000000", performed.ExternalWorkoutId);
        Assert.Equal("7b2281f1-0000-4000-8000-000000000000", performed.ExternalRoutineId);

        var exercise = Assert.Single(performed.Exercises);
        Assert.Equal(
            [11d, 9d, 8d],
            exercise.Sets.OrderBy(set => set.Position).Select(set => set.Reps));
    }

    [Fact]
    public void A_workout_from_no_routine_maps_and_stays_unbound()
    {
        // The ordinary case in every workout observed from a real account. Unbound history is
        // first-class, not a leftover (ADR-019).
        var workout = AWorkout(ASet(0, "normal", 50, 11)) with { RoutineId = null };

        var performed = HevyInboundMapper.ToPerformedWorkout(workout, "user-1", _ => null);

        Assert.Null(performed.ExternalRoutineId);
        Assert.Single(performed.Exercises);
    }

    [Fact]
    public void Set_types_become_our_vocabulary()
    {
        var workout = AWorkout(
            ASet(0, "warmup", 20, 12),
            ASet(1, "normal", 50, 11),
            ASet(2, "dropset", 40, 8),
            ASet(3, "failure", 50, 5));

        var performed = HevyInboundMapper.ToPerformedWorkout(workout, "user-1", _ => null);

        Assert.Equal(
            [SetKind.WarmUp, SetKind.Working, SetKind.DropSet, SetKind.ToFailure],
            performed.Exercises.Single().Sets.OrderBy(set => set.Position).Select(set => set.Kind));
    }

    [Fact]
    public void A_set_type_we_do_not_model_is_refused_rather_than_counted_as_working()
    {
        // Silently counting an unknown set as a working one would inflate every fractional volume
        // figure the system produces (TD-006). The raw payload is retained (ADR-018), so failing
        // loudly loses nothing and a wrong guess would.
        var workout = AWorkout(ASet(0, "cluster", 50, 3));

        Assert.Throws<HevyMappingException>(() =>
            HevyInboundMapper.ToPerformedWorkout(workout, "user-1", _ => null));
    }

    [Fact]
    public void Reported_effort_is_converted_and_an_absent_one_stays_absent()
    {
        var workout = AWorkout(
            ASet(0, "normal", 50, 11, rpe: 8.5),
            ASet(1, "normal", 50, 9));

        var sets = HevyInboundMapper
            .ToPerformedWorkout(workout, "user-1", _ => null)
            .Exercises.Single().Sets
            .OrderBy(set => set.Position)
            .ToList();

        Assert.Equal(1, sets[0].RepsInReserve);   // 8.5 -> discard the "maybe" -> 1
        Assert.Null(sets[1].RepsInReserve);       // reported nothing, which is not "nothing left"
    }

    [Fact]
    public void An_exercise_outside_our_catalogue_maps_with_no_exercise_of_ours()
    {
        // Null is expected and meaningful: a movement the user trained that we do not model. The
        // loud signal TD-004 chose, and what ADR-020 reads as a catalogue gap.
        var performed = HevyInboundMapper.ToPerformedWorkout(
            AWorkout(ASet(0, "normal", 60, 10)), "user-1", _ => null);

        var exercise = Assert.Single(performed.Exercises);
        Assert.Null(exercise.ExerciseId);
        Assert.Equal("79D0BB3A", exercise.ExternalTemplateId);
    }

    [Fact]
    public void An_exercise_in_our_catalogue_maps_by_external_key_and_never_by_title()
    {
        // ADR-002 and standard 9: the lookup is on the identifier. The title on the payload is
        // deliberately one no catalogue row carries, so a title-based match would find nothing
        // and this test would fail.
        var ours = AnExercise();
        var workout = AWorkout(ASet(0, "normal", 50, 11));
        workout = workout with
        {
            Exercises = [new HevyWorkoutExercise(0, "A title we never stored", null, "79D0BB3A", null,
                [ASet(0, "normal", 50, 11)])],
        };

        var performed = HevyInboundMapper.ToPerformedWorkout(
            workout, "user-1", templateId => templateId == "79D0BB3A" ? ours.Id : null);

        Assert.Equal(ours.Id, performed.Exercises.Single().ExerciseId);
    }

    [Fact]
    public void Times_arrive_as_UTC()
    {
        var workout = AWorkout(ASet(0, "normal", 50, 11)) with
        {
            StartTime = new DateTimeOffset(2026, 8, 20, 11, 51, 0, TimeSpan.FromHours(-3)),
        };

        var performed = HevyInboundMapper.ToPerformedWorkout(workout, "user-1", _ => null);

        Assert.Equal(TimeSpan.Zero, performed.StartedAt.Offset);   // root standard 5
        Assert.Equal(14, performed.StartedAt.Hour);
    }
}

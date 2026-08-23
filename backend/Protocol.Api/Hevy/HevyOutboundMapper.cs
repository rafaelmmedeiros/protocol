using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// One of our planned sessions, translated into a Hevy routine. The outward half of the
/// boundary, and the only place that direction is spelled (root standard 17).
/// </summary>
public static class HevyOutboundMapper
{
    /// <summary>Hevy's word for a working set. Ours is <see cref="SetKind.Working"/>.</summary>
    private const string NormalSet = "normal";

    /// <summary>
    /// Translates a session into the payload that creates or replaces its routine.
    /// </summary>
    /// <param name="title">What the routine is called in Hevy. Display only, never read back.</param>
    /// <param name="folderId">The week's folder, when it has one (ADR-015).</param>
    /// <param name="prescriptions">The session's slots, in the order they are to be trained.</param>
    /// <param name="noteFor">
    /// The line of text that carries the prescribed effort to the user, composed in their
    /// language by the caller (ADR-016, root standard 2). It lives outside this mapper because a
    /// translated sentence is a frontend concern that happens to be composed server-side, and a
    /// mapper that formatted prose would be the wrong place to look for it.
    /// </param>
    /// <exception cref="HevyMappingException">
    /// A prescription references an exercise with no external key, which cannot be pushed. Loud
    /// rather than silently substituted (ADR-016).
    /// </exception>
    public static HevyRoutinePayload ToRoutine(
        string title,
        long? folderId,
        IReadOnlyList<GeneratedPrescription> prescriptions,
        Func<GeneratedPrescription, string?> noteFor)
    {
        var exercises = prescriptions
            .OrderBy(prescription => prescription.Position)
            .Select(prescription => ToExercise(prescription, noteFor))
            .ToList();

        return new HevyRoutinePayload(title, folderId, Notes: null, exercises);
    }

    private static HevyRoutineExercise ToExercise(
        GeneratedPrescription prescription,
        Func<GeneratedPrescription, string?> noteFor)
    {
        var templateId = prescription.Exercise?.ExternalTemplateId;

        if (string.IsNullOrWhiteSpace(templateId))
        {
            // An exercise we cannot name to Hevy cannot be pushed. Substituting a neighbour would
            // put a movement in the user's gym that no record chose.
            throw new HevyMappingException(
                $"Exercise {prescription.ExerciseId} has no external template identifier.");
        }

        // Every set of a slot carries the same prescription, so they are identical by
        // construction. The count is ours; the shape is theirs.
        var sets = Enumerable
            .Range(0, prescription.Sets)
            .Select(_ => new HevyRoutineSet(
                Type: NormalSet,
                // Null, not zero, and not a guess. No load is prescribed until the system has
                // watched the user lift, and the load they choose is the observation that makes
                // prescribing one possible (TD-001, ADR-016).
                WeightKg: null,
                // The range goes in rep_range, which their routine sets accept natively. Reps is
                // left null deliberately: a single number is what censors the observation, and a
                // range terminated on effort is what keeps the log readable (ADR-016).
                Reps: null,
                RepRange: new HevyRepRange(prescription.MinReps, prescription.MaxReps)))
            .ToList();

        return new HevyRoutineExercise(
            ExerciseTemplateId: templateId,
            // Never supersetted: TD-013 declined them.
            SupersetId: null,
            RestSeconds: prescription.RestSeconds,
            Notes: noteFor(prescription),
            Sets: sets);
    }
}

/// <summary>
/// A payload that could not be built or read. Thrown rather than returned because every case is
/// a fact about the data that a caller cannot paper over — an exercise with no external key, or
/// a set type we do not model.
/// </summary>
public sealed class HevyMappingException(string message) : Exception(message);

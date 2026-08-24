namespace Protocol.Api.Training;

/// <summary>
/// What a set of prescribed slots credits to each muscle group, kept apart into the direct half
/// and the indirect half.
/// <para>
/// The split is not presentation. An indirect set counts 0.5 (TD-006), and folding the two
/// together produces a number that cannot be argued with: 6.0 built from six direct sets and 6.0
/// built from three direct plus six indirect are different prescriptions, and only the separated
/// figures say which one a muscle received.
/// </para>
/// <para>
/// It takes slots rather than a week so that both a freshly generated <see cref="WeekPlan"/> and a
/// stored week joined against the catalogue can be measured by the same arithmetic — which is what
/// makes a unit test of this meaningful for the response that ships.
/// </para>
/// </summary>
public static class PrescribedVolume
{
    /// <summary>Direct and indirect fractional sets per muscle group, over the muscles actually loaded.</summary>
    public static Dictionary<MuscleGroup, MuscleVolume> ByMuscle(
        IEnumerable<(Exercise Exercise, int Sets)> slots)
    {
        var volumes = new Dictionary<MuscleGroup, MuscleVolume>();

        foreach (var (exercise, sets) in slots)
        {
            foreach (var muscle in exercise.Muscles)
            {
                var current = volumes.GetValueOrDefault(muscle.MuscleGroup);

                volumes[muscle.MuscleGroup] = muscle.Role == MuscleRole.Primary
                    ? current with { Direct = current.Direct + (sets * TrainingPrescription.PrimarySetCredit) }      // TD-006
                    : current with { Indirect = current.Indirect + (sets * TrainingPrescription.SecondarySetCredit) }; // TD-006
            }
        }

        return volumes;
    }

    /// <summary>
    /// The muscle groups no exercise in a catalogue trains <i>directly</i>. They can only ever
    /// reach volume through 0.5-weighted secondary roles, so no amount of training more closes the
    /// gap — which is why they are reported apart from a shortfall the user can act on (TD-013).
    /// </summary>
    public static IReadOnlyList<MuscleGroup> UncoveredBy(IEnumerable<Exercise> catalogue)
    {
        var direct = catalogue
            .Select(exercise => exercise.Muscles.Single(muscle => muscle.Role == MuscleRole.Primary).MuscleGroup)
            .ToHashSet();

        return [.. Enum.GetValues<MuscleGroup>().Where(muscle => !direct.Contains(muscle)).Order()];
    }
}

/// <summary>
/// One muscle group's share of a plan, with the two halves kept apart (TD-006).
/// </summary>
public readonly record struct MuscleVolume(decimal Direct, decimal Indirect)
{
    /// <summary>What the volume arithmetic actually compares against a target.</summary>
    public decimal Total => Direct + Indirect;
}

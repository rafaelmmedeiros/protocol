namespace Protocol.Api.Training;

/// <summary>
/// How a session's minutes convert into slots (TD-012).
/// <para>
/// The additive model itself, not the linear shortcut TD-012 also states: the shortcut assumes a
/// representative slot ordering, and a greedy fill does not guarantee one. Rest is 74-79% of a
/// session's clock, so the accuracy that matters is in the rest term and nowhere else.
/// </para>
/// </summary>
public static class SessionTimeBudget
{
    /// <summary>
    /// Back-calculated from two trials that published a protocol alongside a measured session
    /// duration, and free for growth across 0.5-8 s per repetition (TD-012).
    /// </summary>
    public const int RepetitionSeconds = 3; // TD-012

    /// <summary>
    /// Finding the rack, loading the bar, adjusting the seat. **An engineering estimate with no
    /// source behind it** — the calibration trials had equipment reserved, so nothing measures
    /// this. It is the model's weakest number and the first to raise if sessions run long
    /// (TD-012).
    /// </summary>
    public const int TransitionSeconds = 60; // TD-012 (engineering estimate, not evidence)

    /// <summary>
    /// Two ramping sets and the approach to the first working set. **Also an engineering
    /// estimate**: no paper reports the wall-clock cost of a warm-up. No general warm-up is
    /// generated at all, and ramping is specific to the first heavy compound (TD-012).
    /// </summary>
    public const int WarmUpSeconds = 180; // TD-012 (engineering estimate, not evidence)

    /// <summary>
    /// What one slot costs: its sets, the rest between them, and the transition to the next
    /// exercise. Rest <i>after</i> the final set is charged as the transition rather than twice.
    /// </summary>
    public static int SlotCostSeconds(OrderClass orderClass, int sets, int restSeconds)
    {
        var prescription = TrainingPrescription.For(orderClass);
        var secondsPerSet = (prescription.MinReps + prescription.MaxReps) * RepetitionSeconds / 2;

        return (sets * secondsPerSet) + ((sets - 1) * restSeconds) + TransitionSeconds;
    }

    /// <summary>
    /// The time available for slots once warm-up is reserved.
    /// <para>
    /// Warm-up is reserved for every session rather than only for those that end up containing a
    /// <see cref="OrderClass.CompoundPrimary"/> slot, which TD-012 is what strictly specifies.
    /// The difference over-predicts session length slightly, and over-predicting is the safe
    /// direction — sessions come in short rather than long.
    /// </para>
    /// </summary>
    public static int SlotSecondsAvailable(int sessionDurationSeconds) =>
        Math.Max(0, sessionDurationSeconds - WarmUpSeconds);
}

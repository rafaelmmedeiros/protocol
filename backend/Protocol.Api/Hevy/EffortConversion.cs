namespace Protocol.Api.Hevy;

/// <summary>
/// Hevy's RPE against our repetitions in reserve (TD-017).
/// <para>
/// One place per direction, at the boundary, and nowhere else (root standard 17). The domain
/// stores a count of repetitions and has no symbol named for a rating of perceived exertion —
/// which is what <c>BoundaryIsolationTests</c> exists to keep true.
/// </para>
/// </summary>
public static class EffortConversion
{
    /// <summary>
    /// Hevy's RPE to our repetitions in reserve, resolving toward **less** reserve.
    /// <para>
    /// Arithmetically <c>10 - ceil(rpe)</c>. In the engineer's own terms it is *discard the
    /// "maybe"*, and those are the same rule: every hedge in Hevy's wording sits on the upper
    /// value — "1 more rep, maybe 2", never "2, maybe 1" — so dropping it always yields the
    /// lower count, the reading in which the lifter was closer to failure (TD-017).
    /// </para>
    /// <para>
    /// Returns null for an absent value, and <b>only</b> for an absent value. An anchor Hevy does
    /// not offer is refused rather than rounded into range: a new anchor is a decision, not an
    /// input.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not one of Hevy's eight anchors.
    /// </exception>
    public static int? ToRepsInReserve(double? rpe)
    {
        if (rpe is not { } value)
        {
            // Absent, not zero. "Reported nothing" and "reported nothing left" are opposite
            // claims about how hard the user worked, and collapsing them would be a lie the
            // progression rule reads as fact.
            return null;
        }

        if (!Anchors.Contains(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rpe),
                value,
                "Not one of Hevy's RPE anchors. A new anchor is a decision, not an input (TD-017).");
        }

        // 10 and 9.5 both give 0, 9 and 8.5 both give 1, and so on. A repetition in reserve is a
        // count; there is no half of one, and this method can never produce one.
        return 10 - (int)Math.Ceiling(value);
    }

    /// <summary>
    /// Our repetitions in reserve to Hevy's RPE. Exact, integer to integer, no interpretation.
    /// <para>
    /// **This direction has no consumer, by design.** A Hevy routine set carries no <c>rpe</c>
    /// field, because effort is feedback and a plan does not carry an observation (ADR-016).
    /// Read that record before wiring this into anything: writing a prescribed target into a
    /// field that means feedback would make the import read our own prescription back as the
    /// user's report, and the gap the whole loop exists to expose would close to zero.
    /// </para>
    /// </summary>
    public static double ToRpe(int repsInReserve) => 10 - repsInReserve;

    /// <summary>
    /// The eight values Hevy's scale offers. Half points only from 7.5 upward, which is the
    /// signature of the RIR-based scale rather than a perceived-exertion one.
    /// </summary>
    public static readonly IReadOnlySet<double> Anchors =
        new HashSet<double> { 6, 7, 7.5, 8, 8.5, 9, 9.5, 10 };
}

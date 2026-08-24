namespace Protocol.Api.Training;

/// <summary>
/// Every number the generator prescribes, each beside the record that decided it (root
/// standard 15). Nothing here was recalled: a number without a <c>TD-###</c> at the line cannot
/// be told apart later from one that was.
/// </summary>
public static class TrainingPrescription
{
    /// <summary>
    /// A set where the muscle is secondary counts half. This is the volume literature's own
    /// convention, and it lives in one constant rather than on catalogue rows so that it can be
    /// revised and audited in one place (TD-006).
    /// </summary>
    public const decimal SecondarySetCredit = 0.5m; // TD-006

    /// <summary>A set where the muscle is primary counts whole (TD-006).</summary>
    public const decimal PrimarySetCredit = 1.0m; // TD-006

    /// <summary>
    /// Six fractional sets per muscle group per week, uniform across every modelled muscle.
    /// Superseded TD-008's eight, because at eight a user training three times for forty
    /// minutes could never reach the target in any arrangement of their time (TD-014).
    /// </summary>
    public const decimal WeeklyTargetFractionalSets = 6.0m; // TD-014

    /// <summary>
    /// What a muscle group receives when the declared minutes are there to pay for it. The
    /// weekly figure is a band, not a number: <see cref="WeeklyTargetFractionalSets"/> is what
    /// every supported configuration reaches, and this is the most a lifter with time to spare
    /// gets. It is TD-008's own eight — defended on concavity, given up by TD-014 for
    /// reachability rather than for dose, and restored here only where the minutes exist.
    /// <para>
    /// <b>The cut ladder never runs on its behalf.</b> Compressing a researched rest interval to
    /// buy volume above the researched target would trade a measured quantity for a convention,
    /// in that direction (TD-021).
    /// </para>
    /// </summary>
    public const decimal WeeklyCeilingFractionalSets = 8.0m; // TD-021

    /// <summary>
    /// Below four fractional sets the prescription leaves the region every dose-response model
    /// behind it was fitted in. A muscle under this floor is a surfaced failure, never a silent
    /// shortfall (TD-008).
    /// </summary>
    public const decimal WeeklyFloorFractionalSets = 4.0m; // TD-008

    /// <summary>
    /// Remmert's point of undetectable superiority. It does not bind at the supported volumes
    /// and is asserted as a guard against a future volume rise, not enforced as a rule (TD-008).
    /// </summary>
    public const decimal PerSessionCapFractionalSets = 11.0m; // TD-008

    /// <summary>
    /// Three sets per slot. Pure convention: the evidence constrains weekly volume, not sets
    /// per slot, and three divides cleanly into the weekly target (TD-008).
    /// </summary>
    public const int SetsPerSlot = 3; // TD-008

    /// <summary>
    /// Two sets per slot, used only by the cut ladder when the time budget cannot otherwise
    /// reach the floor. Spread evenly across all slots, never concentrated (TD-013).
    /// </summary>
    public const int ReducedSetsPerSlot = 2; // TD-013

    /// <summary>
    /// Two sets per slot, in a slot bought <i>above</i> the guaranteed target because the
    /// declared minutes were there to pay for it — the exact opposite of
    /// <see cref="ReducedSetsPerSlot"/>, which is what the ladder falls back to when they were
    /// not.
    /// <para>
    /// The value closes the arithmetic rather than expressing a preference: a slot of three sets
    /// credits 3.0 to its primary muscle, so a muscle at the guaranteed 6.0 would land at 9.0 and
    /// the band from 6.0 to 8.0 is narrower than one slot. At two sets it lands on 8.0 exactly.
    /// </para>
    /// <para>
    /// <b>Equal to <see cref="ReducedSetsPerSlot"/> today and deliberately not merged with it.</b>
    /// They answer different questions and nothing should make one follow the other (TD-022).
    /// </para>
    /// </summary>
    public const int CeilingSetsPerSlot = 2; // TD-022

    /// <summary>
    /// What is prescribed into a slot of a given <see cref="OrderClass"/>.
    /// <para>
    /// Repetition ranges are convention that a genuine null permits — growth is equivalent
    /// across roughly 5-30 repetitions when sets are near failure (TD-009). The one
    /// evidence-linked choice is keeping the primary compound under ~12 repetitions, where
    /// proximity-to-failure judgement is most accurate and a missed target costs most.
    /// </para>
    /// <para>
    /// Rest descends by load and discomfort, not because compounds recover slower — the
    /// best-controlled comparison found the one-minute penalty in the isolation exercise as
    /// much as the compound (TD-011).
    /// </para>
    /// <para>
    /// Proximity to failure does <b>not</b> vary by order_class, and used to. TD-010 graded
    /// three by exercise type; TD-018 withdrew that gradient because its accuracy argument runs
    /// backwards — RIR judgement is most accurate under heavy load and fewest repetitions, not
    /// least — and because ACSM 2026 prescribes one uniform target. Anything that reintroduces a
    /// per-order_class RIR should read TD-018 before doing so.
    /// </para>
    /// </summary>
    public static SlotPrescription For(OrderClass orderClass) => orderClass switch
    {
        OrderClass.CompoundPrimary => new SlotPrescription(
            MinReps: 6, MaxReps: 10,            // TD-009
            RepsInReserve: 2,                   // TD-018
            RestSeconds: 180),                  // TD-011
        OrderClass.CompoundSecondary => new SlotPrescription(
            MinReps: 8, MaxReps: 12,            // TD-009
            RepsInReserve: 2,                   // TD-018
            RestSeconds: 150),                  // TD-011
        OrderClass.Isolation => new SlotPrescription(
            MinReps: 10, MaxReps: 15,           // TD-009
            RepsInReserve: 2,                   // TD-018
            RestSeconds: 90),                   // TD-011
        _ => throw new ArgumentOutOfRangeException(nameof(orderClass)),
    };

    /// <summary>
    /// The rest floor. Sixty seconds is the one interval the acute evidence argues against — it
    /// costs repetitions in isolation exercises as much as in compounds — so the cut ladder
    /// stops here and takes a set instead (TD-011).
    /// </summary>
    public const int RestFloorSeconds = 90; // TD-011
}

/// <summary>What a slot prescribes, once its exercise is chosen.</summary>
public sealed record SlotPrescription(int MinReps, int MaxReps, int RepsInReserve, int RestSeconds);

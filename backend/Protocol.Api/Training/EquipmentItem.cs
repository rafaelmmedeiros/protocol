namespace Protocol.Api.Training;

/// <summary>
/// A thing a gym contains, at the granularity a person can actually answer for.
/// <para>
/// This is **not** <see cref="Equipment"/>. That field discriminates a *variant* — a barbell
/// curl from a dumbbell curl — and `TD-005` made it single-valued for exactly that job. This
/// vocabulary answers a different question: what has to be present for a movement to be
/// performed at all. A barbell bench press is discriminated by its barbell and requires a
/// barbell, plates **and** a bench. Conflating the two is what `ADR-010` got wrong (`ADR-013`).
/// </para>
/// <para>
/// It holds exactly what the catalogue requires and no more. A value with no exercise behind it
/// is a checkbox that does nothing, which is worse than an absent one — so a machine enters
/// this list in the same commit as the first exercise that needs it.
/// </para>
/// </summary>
public enum EquipmentItem
{
    /// <summary>
    /// Nothing at all. Present so that a bodyweight exercise carries an explicit requirement
    /// rather than an empty set — an empty set cannot be told apart from a row nobody curated,
    /// and `TD-005` already names miscuration as the soft spot of this catalogue.
    /// </summary>
    Bodyweight,

    Barbell,
    WeightPlates,
    Dumbbells,

    /// <summary>A bench to lie on. Flat is enough.</summary>
    Bench,

    /// <summary>
    /// A bench that inclines. Held separately rather than implying <see cref="Bench"/>, so that
    /// an inclined movement requires both and no implication rule has to exist. Someone who owns
    /// an adjustable bench owns both; someone with a flat bench owns one.
    /// </summary>
    AdjustableBench,

    SquatRack,
    PullUpBar,
    CableStation,
    LatPulldownStation,
}

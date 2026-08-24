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

    // --------------------------------------------------------------------------------------
    // Machines. One value per machine, because that is the unit a gym has or does not have
    // (ADR-022, applying ADR-013). A gym with a leg press and no leg curl is ordinary, and a
    // coarser vocabulary would assert the second — the invisible failure TD-019 is built around.
    //
    // Added because a catalogue row requires them and for no other reason: an unused value is a
    // checkbox asking a question the system cannot act on.
    //
    // Their order is irrelevant and must stay that way. These are stored as text, never as an
    // ordinal, so inserting a value cannot change what a row written last month means
    // (root standard 7).
    // --------------------------------------------------------------------------------------

    LegPressMachine,
    HackSquatMachine,
    LegExtensionMachine,

    /// <summary>
    /// Seated and lying leg curls are separate machines, and a gym can own one without the other.
    /// Collapsing them would prescribe the one that is missing — and between them they close the
    /// `knee_flexion` hole TD-004 named in M1 and TD-019 still carries.
    /// </summary>
    SeatedLegCurlMachine,
    LyingLegCurlMachine,
    StandingLegCurlMachine,

    HipAbductionMachine,

    /// <summary>Seated and standing calf machines load different joint angles and are bought separately.</summary>
    SeatedCalfRaiseMachine,
    StandingCalfRaiseMachine,

    ChestPressMachine,

    /// <summary>
    /// One machine, both directions. A pec deck runs forwards for a chest fly and backwards for a
    /// rear delt fly, so owning it means owning both — which is why they share a value rather than
    /// each getting one.
    /// </summary>
    PecDeckMachine,

    SeatedRowMachine,
    HighRowMachine,
    ShoulderPressMachine,
    PreacherCurlMachine,
    AbdominalMachine,
    BackExtensionBench,

    /// <summary>
    /// A guided bar, not a selectorised stack — kept separate because a gym often has one and none
    /// of the others, and because what it makes possible is a squat rather than a machine pattern.
    /// </summary>
    SmithMachine,
}

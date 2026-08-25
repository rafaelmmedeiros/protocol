namespace Protocol.Api.Training;

/// <summary>
/// A week of training, as the generator produces it. Pure data: no database, no HTTP, nothing
/// that could not be recomputed from a profile and a catalogue (ADR-005, ADR-006).
/// </summary>
/// <param name="Shortfalls">
/// Muscles the catalogue can train that still finished below TD-008's floor — a time-budget gap
/// the user can act on.
/// </param>
/// <param name="UncoveredMuscles">
/// Muscles no exercise in the catalogue trains <i>directly</i>, so they reach volume only
/// through 0.5-weighted secondary roles. It is computed over the exercises <i>that user</i> can
/// perform, so it is not one fixed list. On TD-004's assumed gym it is <c>SpinalErectors</c> and
/// <c>Adductors</c> — <c>Forearms</c> left it in M4 when the wrist curls landed, and they need only
/// a barbell — and a user whose equipment reaches M4's machine rows (TD-019) loses
/// <c>SpinalErectors</c> to the back extension too. A catalogue coverage failure, surfaced rather than patched: padding the
/// catalogue to hide it would be the wrong fix. These never drive the cut ladder, because no
/// amount of cutting closes a gap in what the gym contains.
/// </param>
public sealed record WeekPlan(
    IReadOnlyList<PlannedSession> Sessions,
    IReadOnlyList<MuscleShortfall> Shortfalls,
    IReadOnlyList<MuscleGroup> UncoveredMuscles,
    CutLevel CutApplied)
{
    /// <summary>
    /// True when every muscle the catalogue can actually train reached TD-008's floor. A week
    /// with shortfalls is still returned rather than refused — the user gets the best week their
    /// time allows, and the gap is surfaced as data for the frontend to say out loud (TD-013).
    /// </summary>
    public bool MeetsFloor => Shortfalls.Count == 0;
}

/// <summary>
/// One session of the queue. It has a position and no date: a plan is an ordered list of
/// sessions, and the next one is whichever is next unfinished (ADR-027).
/// <para>
/// The weekday it used to carry was never a training decision — with weekly volume equated,
/// distribution across days does not change growth — and it produced a permanent per-muscle
/// deficit whenever the same session was the one life kept taking.
/// </para>
/// </summary>
public sealed record PlannedSession(
    int Position,
    SessionKind Kind,
    IReadOnlyList<PlannedSlot> Slots);

/// <summary>
/// A position in a session holding one exercise and the prescription attached to it — the unit
/// TD-005 defines and TD-013 cuts.
/// </summary>
public sealed record PlannedSlot(
    int Position,
    Exercise Exercise,
    int Sets,
    SlotPrescription Prescription);

/// <summary>
/// A muscle the catalogue <i>can</i> train that still finished the week below TD-008's floor of
/// four fractional sets. This is a time-budget failure and the user can act on it: train longer,
/// or train more days.
/// <para>
/// Kept apart from <see cref="WeekPlan.UncoveredMuscles"/> on purpose. The two look
/// identical in the data and are different problems — one is fixed by the user, the other only
/// by changing what equipment is assumed, and mixing them would make the cut ladder chase a gap
/// no amount of cutting can close.
/// </para>
/// </summary>
public sealed record MuscleShortfall(MuscleGroup MuscleGroup, decimal FractionalSets);

/// <summary>
/// Why a slot is the size it is — the distinction TD-022 created and a reader cannot infer from
/// the set count alone.
/// <para>
/// Two sets means opposite things one record apart: <see cref="Full"/> at reduced size is what
/// TD-013's ladder falls back to when the time budget could not otherwise reach the floor, while
/// <see cref="Ceiling"/> is what a lifter gets *because* they had minutes to spare. Without this
/// the screen shows a two-set slot and the reader takes a generous week for a cut one.
/// </para>
/// </summary>
public enum SlotKind
{
    /// <summary>Drawn to reach the guaranteed target (TD-014). Carries the week's set count.</summary>
    Full,

    /// <summary>Bought above the guaranteed target because the declared minutes were there (TD-022).</summary>
    Ceiling,
}

/// <summary>
/// What a user declared about a session of the queue. Neither writes anything into imported
/// history: both are statements about the plan (root standard 7).
/// </summary>
public enum SessionDeclaration
{
    /// <summary>Trained, with nothing bound to it — the fallback `ADR-028` exists for.</summary>
    Marked,

    /// <summary>
    /// Passed over. The queue advances and the session's volume never arrives, which is why a
    /// skip is stored rather than silent: deferred volume and skipped volume are different
    /// failures and a report that adds them together flatters the system (`ADR-032`).
    /// </summary>
    Skipped,
}

/// <summary>
/// How a session stands in the queue, as the API reports it. Three of the four are ways out; the
/// fourth is still ahead.
/// </summary>
public enum SessionOutcome
{
    /// <summary>Still in the queue. The first pending session is the next one to train.</summary>
    Pending,

    /// <summary>A logged workout carries this session's routine id (`ADR-019`).</summary>
    Bound,

    /// <summary>Declared trained (`ADR-028`).</summary>
    Marked,

    /// <summary>Declared skipped (`ADR-032`). Never read as a completion.</summary>
    Skipped,
}

/// <summary>
/// How far down TD-013's cut ladder the generator had to go for the week to fit.
/// <para>
/// Reported rather than hidden, because it is not an edge case: at three sessions of forty
/// minutes the ladder runs in full on an entirely ordinary configuration.
/// </para>
/// </summary>
public enum CutLevel
{
    /// <summary>Nothing cut. Prescribed rest, three sets a slot.</summary>
    None,

    /// <summary>Rest trimmed to TD-011's floor of ninety seconds (TD-013, step 1).</summary>
    RestToFloor,

    /// <summary>Rest at the floor and two sets a slot, spread evenly (TD-013, step 3).</summary>
    RestToFloorAndFewerSets,
}

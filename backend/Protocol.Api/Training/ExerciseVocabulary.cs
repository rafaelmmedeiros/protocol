namespace Protocol.Api.Training;

/// <summary>
/// The movement a slot asks for. Deliberately finer than squat/hinge/push/pull: a coarse
/// taxonomy cannot express that a lateral raise and an overhead press are different slots,
/// which is the single most important thing selection needs from this field (TD-005).
/// <para>
/// <c>Carry</c> is deliberately absent: it has no hypertrophy role in the corpus and does not
/// accept a sets/reps/RIR prescription cleanly (TD-005).
/// </para>
/// </summary>
public enum MovementPattern
{
    // Lower
    Squat,
    Hinge,
    Lunge,
    KneeExtension,
    KneeFlexion,
    HipExtension,
    HipAbduction,
    CalfRaise,

    // Upper push
    HorizontalPush,
    VerticalPush,
    HorizontalAdduction,
    LateralRaise,
    ElbowExtension,

    // Upper pull
    HorizontalPull,
    VerticalPull,
    HorizontalAbduction,
    ElbowFlexion,
    Shrug,

    // Trunk
    TrunkFlexion,
    AntiExtension,
    AntiRotation,
}

/// <summary>
/// Our muscle vocabulary (TD-005). Sixteen groups, and the granularity is load-bearing rather
/// than cosmetic: the deltoid is three groups because a push day delivers large indirect
/// front-delt volume and near-zero side-delt volume, so collapsing them makes the fractional
/// count (TD-006) wrong in a direction nothing surfaces.
/// <para>
/// Hevy collapses all three deltoid heads into a single <c>shoulders</c> value, which is one
/// reason this vocabulary is ours and is never derived from theirs (ADR-002, TD-015).
/// </para>
/// </summary>
public enum MuscleGroup
{
    Chest,
    FrontDelts,
    SideDelts,
    RearDelts,
    Lats,
    UpperBack,
    Biceps,
    Triceps,
    Forearms,
    Quads,
    Hamstrings,
    Glutes,
    Calves,
    Abs,
    SpinalErectors,
    Adductors,
}

/// <summary>
/// How a muscle is loaded by an exercise. Stored as an enum rather than a per-row weight on
/// purpose: the 0.5 credit for an indirect set is a single training judgement (TD-006) that
/// belongs in one constant, not scattered across catalogue rows where it cannot be revised or
/// audited (TD-005).
/// <para>
/// <c>Secondary</c> means "meaningfully loaded through a substantial range", not "anything that
/// contracts". Erectors are secondary on a squat and a row; they are not secondary on a leg
/// press. Tagging this inconsistently moves every volume number the product produces, and
/// unlike a wrong constant it will not be visible in a diff (TD-005).
/// </para>
/// </summary>
public enum MuscleRole
{
    Primary,
    Secondary,
}

/// <summary>Whether the movement crosses one joint or several (TD-005).</summary>
public enum Mechanic
{
    Compound,
    Isolation,
}

/// <summary>
/// What the movement is loaded with. Single-valued on purpose: a barbell bench press and a
/// dumbbell bench press are two rows, not one row with two options, because they are not
/// identical muscle maps and because it keeps M2's equipment filter trivial (TD-005, TD-015).
/// <para>
/// This vocabulary cannot be imported from Hevy, which collapses cable, Smith machine and
/// selectorised machine into a single <c>machine</c> value — leaving TD-004's assumed gym
/// (cables yes, selectorised machines no) inexpressible in their terms.
/// </para>
/// </summary>
public enum Equipment
{
    Barbell,
    Dumbbell,
    Machine,
    Cable,
    SmithMachine,
    Bodyweight,

    /// <summary>Bodyweight that accepts added load: a pull-up bar, a dip station.</summary>
    BodyweightLoadable,
    Band,
    Kettlebell,
    Other,
}

/// <summary>
/// Where in a session the exercise belongs (TD-007). Stored rather than derived from
/// <see cref="Mechanic"/>, because the split between a primary and a secondary compound is
/// exactly the judgement being recorded (TD-005).
/// </summary>
public enum OrderClass
{
    /// <summary>A heavy loaded bilateral pattern.</summary>
    CompoundPrimary,

    /// <summary>Everything else multi-joint.</summary>
    CompoundSecondary,

    /// <summary>Single-joint accessories.</summary>
    Isolation,
}

/// <summary>
/// Whether one prescribed set trains one side or both (TD-005). Stored not because it affects
/// growth — it does not — but because a unilateral set costs two sets of time, and because Hevy
/// logs per side, so the import mapping and the volume count must agree on what one prescribed
/// set means.
/// </summary>
public enum Laterality
{
    Bilateral,
    Unilateral,
}

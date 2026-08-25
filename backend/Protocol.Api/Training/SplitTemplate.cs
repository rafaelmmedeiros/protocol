namespace Protocol.Api.Training;

/// <summary>What a session trains.</summary>
public enum SessionKind
{
    FullBody,
    Upper,
    Lower,
    Push,
    Pull,
    Legs,
}

/// <summary>One day of a split: what it trains and when.</summary>
public sealed record SplitDay(DayOfWeek Day, SessionKind Kind);

/// <summary>
/// The templates a frequency may be run with (TD-023). Named by their shape rather than by a
/// number, because the number is the frequency and two templates share it.
/// </summary>
public enum SplitTemplateId
{
    FullBodyX2,
    FullBodyX3,
    UpperLowerFull,
    UpperLowerX2,
    PushPullLegsFull,
    UpperLowerUpperLowerFull,
    UpperLowerPushPullLegs,
    PushPullLegsX2,
    UpperLowerX3,
}

/// <summary>
/// The frequency-to-split mapping (TD-003). Total over every frequency TD-002 admits, so an
/// unmapped frequency is unreachable rather than a runtime failure.
/// <para>
/// This is not a training decision and must never be presented as one. Once weekly volume is
/// equated, split organisation has no detectable effect on hypertrophy — the templates are
/// picked because they repeat weekly, distribute rest, and land per-muscle frequency at 2-3x.
/// </para>
/// </summary>
public static class SplitTemplate
{
    /// <summary>
    /// What each frequency may be run with, default first (TD-023). Every template here was
    /// measured to give every modelled muscle group at least 2x per cycle; `Upper / Lower` at
    /// two sessions was measured too and fails at 1x, which is why that row offers no choice.
    /// <para>
    /// Nothing may present one of these as better for growth. Once weekly volume is equated,
    /// split organisation has no detectable effect — the choice is scheduling and preference,
    /// and a recommendation badge is exactly where that claim would come back.
    /// </para>
    /// </summary>
    public static IReadOnlyList<SplitTemplateId> Admitted(int daysPerWeek) => daysPerWeek switch
    {
        // TD-023
        2 => [SplitTemplateId.FullBodyX2],
        3 => [SplitTemplateId.FullBodyX3, SplitTemplateId.UpperLowerFull],
        4 => [SplitTemplateId.UpperLowerX2, SplitTemplateId.PushPullLegsFull],
        5 => [SplitTemplateId.UpperLowerUpperLowerFull, SplitTemplateId.UpperLowerPushPullLegs],
        6 => [SplitTemplateId.PushPullLegsX2, SplitTemplateId.UpperLowerX3],
        _ => throw new ArgumentOutOfRangeException(
            nameof(daysPerWeek),
            daysPerWeek,
            "TD-002 supports 2 to 6 training days; validation should have rejected this."),
    };

    /// <summary>
    /// What a frequency runs with when the user has not chosen. It is TD-003's answer unchanged,
    /// which is what makes a profile that never chose generate exactly the week it always did.
    /// </summary>
    public static SplitTemplateId Default(int daysPerWeek) => Admitted(daysPerWeek)[0]; // TD-023

    /// <summary>
    /// The one place a null choice becomes a template (ADR-030). Null means "whatever this
    /// frequency maps to", and resolving it in two places is how the two would disagree.
    /// <para>
    /// A stored choice that the current frequency does not admit falls back to the default. The
    /// endpoint rejects that combination, so this is reachable only through a row whose
    /// frequency changed without its split — and falling back beats generating from a template
    /// whose session count no longer matches the frequency, which would break the property
    /// TD-024 leans on.
    /// </para>
    /// </summary>
    public static SplitTemplateId Resolve(SplitTemplateId? chosen, int daysPerWeek) =>
        chosen is { } value && Admitted(daysPerWeek).Contains(value)
            ? value
            : Default(daysPerWeek); // ADR-030

    /// <summary>
    /// Every template starts on Monday and repeats weekly (root standard 6). Rotating splits on
    /// a six-day cycle are excluded for that reason alone: a week that does not align to the
    /// calendar week makes "which week did this session belong to" unanswerable, and every
    /// later analysis stands on that question.
    /// </summary>
    public static IReadOnlyList<SplitDay> For(SplitTemplateId template) => template switch
    {
        // TD-023. Day assignment follows TD-003's patterns: Monday-anchored, rest distributed
        // rather than trailing.
        SplitTemplateId.FullBodyX2 => For(2),
        SplitTemplateId.FullBodyX3 => For(3),
        SplitTemplateId.UpperLowerX2 => For(4),
        SplitTemplateId.UpperLowerUpperLowerFull => For(5),
        SplitTemplateId.PushPullLegsX2 => For(6),

        SplitTemplateId.UpperLowerFull =>
        [
            new(DayOfWeek.Monday, SessionKind.Upper),
            new(DayOfWeek.Wednesday, SessionKind.Lower),
            new(DayOfWeek.Friday, SessionKind.FullBody),
        ],
        SplitTemplateId.PushPullLegsFull =>
        [
            new(DayOfWeek.Monday, SessionKind.Push),
            new(DayOfWeek.Tuesday, SessionKind.Pull),
            new(DayOfWeek.Thursday, SessionKind.Legs),
            new(DayOfWeek.Friday, SessionKind.FullBody),
        ],
        SplitTemplateId.UpperLowerPushPullLegs =>
        [
            new(DayOfWeek.Monday, SessionKind.Upper),
            new(DayOfWeek.Tuesday, SessionKind.Lower),
            new(DayOfWeek.Thursday, SessionKind.Push),
            new(DayOfWeek.Friday, SessionKind.Pull),
            new(DayOfWeek.Saturday, SessionKind.Legs),
        ],
        SplitTemplateId.UpperLowerX3 =>
        [
            new(DayOfWeek.Monday, SessionKind.Upper),
            new(DayOfWeek.Tuesday, SessionKind.Lower),
            new(DayOfWeek.Wednesday, SessionKind.Upper),
            new(DayOfWeek.Thursday, SessionKind.Lower),
            new(DayOfWeek.Friday, SessionKind.Upper),
            new(DayOfWeek.Saturday, SessionKind.Lower),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(template)),
    };

    /// <summary>
    /// The frequency-to-shape mapping TD-003 decided, now the default column of TD-023's table.
    /// </summary>
    public static IReadOnlyList<SplitDay> For(int daysPerWeek) => daysPerWeek switch
    {
        // TD-003
        2 =>
        [
            new(DayOfWeek.Monday, SessionKind.FullBody),
            new(DayOfWeek.Thursday, SessionKind.FullBody),
        ],
        3 =>
        [
            new(DayOfWeek.Monday, SessionKind.FullBody),
            new(DayOfWeek.Wednesday, SessionKind.FullBody),
            new(DayOfWeek.Friday, SessionKind.FullBody),
        ],
        4 =>
        [
            new(DayOfWeek.Monday, SessionKind.Upper),
            new(DayOfWeek.Tuesday, SessionKind.Lower),
            new(DayOfWeek.Thursday, SessionKind.Upper),
            new(DayOfWeek.Friday, SessionKind.Lower),
        ],
        5 =>
        [
            new(DayOfWeek.Monday, SessionKind.Upper),
            new(DayOfWeek.Tuesday, SessionKind.Lower),
            new(DayOfWeek.Thursday, SessionKind.Upper),
            new(DayOfWeek.Friday, SessionKind.Lower),
            new(DayOfWeek.Saturday, SessionKind.FullBody),
        ],
        6 =>
        [
            new(DayOfWeek.Monday, SessionKind.Push),
            new(DayOfWeek.Tuesday, SessionKind.Pull),
            new(DayOfWeek.Wednesday, SessionKind.Legs),
            new(DayOfWeek.Thursday, SessionKind.Push),
            new(DayOfWeek.Friday, SessionKind.Pull),
            new(DayOfWeek.Saturday, SessionKind.Legs),
        ],
        _ => throw new ArgumentOutOfRangeException(
            nameof(daysPerWeek),
            daysPerWeek,
            "TD-002 supports 2 to 6 training days; validation should have rejected this."),
    };

    private static readonly MuscleGroup[] LowerBody =
    [
        MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes,
        MuscleGroup.Calves, MuscleGroup.Adductors, MuscleGroup.SpinalErectors,
    ];

    private static readonly MuscleGroup[] UpperBody =
    [
        MuscleGroup.Chest, MuscleGroup.FrontDelts, MuscleGroup.SideDelts, MuscleGroup.RearDelts,
        MuscleGroup.Lats, MuscleGroup.UpperBack, MuscleGroup.Biceps, MuscleGroup.Triceps,
        MuscleGroup.Forearms,
    ];

    /// <summary>
    /// Which muscles a session may train. Abs sit with the lower-body sessions so that every
    /// template touches them somewhere without displacing upper-body work.
    /// </summary>
    public static IReadOnlyCollection<MuscleGroup> ScopeOf(SessionKind kind) => kind switch
    {
        SessionKind.FullBody => Enum.GetValues<MuscleGroup>(),
        SessionKind.Upper => UpperBody,
        SessionKind.Lower => [.. LowerBody, MuscleGroup.Abs],
        SessionKind.Legs => [.. LowerBody, MuscleGroup.Abs],
        SessionKind.Push =>
        [
            MuscleGroup.Chest, MuscleGroup.FrontDelts, MuscleGroup.SideDelts, MuscleGroup.Triceps,
        ],
        SessionKind.Pull =>
        [
            MuscleGroup.Lats, MuscleGroup.UpperBack, MuscleGroup.RearDelts,
            MuscleGroup.Biceps, MuscleGroup.Forearms,
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

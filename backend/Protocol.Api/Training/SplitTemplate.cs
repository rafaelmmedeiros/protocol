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
    /// Every template starts on Monday and repeats weekly (root standard 6). Rotating splits on
    /// a six-day cycle are excluded for that reason alone: a week that does not align to the
    /// calendar week makes "which week did this session belong to" unanswerable, and every
    /// later analysis stands on that question.
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

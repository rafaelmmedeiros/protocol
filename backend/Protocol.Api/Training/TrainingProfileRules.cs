namespace Protocol.Api.Training;

/// <summary>
/// What a training profile is allowed to say. Pure: no I/O, no database, no HTTP — so the
/// bounds every generated week stands on can be tested without a container.
/// </summary>
public static class TrainingProfileRules
{
    /// <summary>
    /// Two sessions a week is the floor. One is the single frequency where the weekly volume
    /// bound and the per-session ceiling collide, and no position stand endorses it (TD-002).
    /// </summary>
    public const int MinDaysPerWeek = 2; // TD-002

    /// <summary>
    /// Six is the ceiling, and it is a product bound rather than an evidential one: no trial
    /// tests seven days volume-equated against six, and nothing says seven is harmful — there
    /// is simply no benefit to find and no room to reschedule a missed session (TD-002).
    /// </summary>
    public const int MaxDaysPerWeek = 6; // TD-002

    /// <summary>
    /// 25 minutes. Below this the generator cannot deliver the minimum viable session — one
    /// leg press pattern, one upper push, one upper pull — without breaking TD-011's rest floor
    /// (TD-012).
    /// </summary>
    public const int MinSessionDurationSeconds = 1_500; // TD-012

    /// <summary>
    /// 120 minutes. Also a product bound: at the supported volume the target binds long before
    /// the clock does, so this exists to reject typos rather than to cap training (TD-012).
    /// </summary>
    public const int MaxSessionDurationSeconds = 7_200; // TD-012

    /// <summary>The only goal M1 programmes for (ADR-004).</summary>
    public const TrainingGoal SupportedGoal = TrainingGoal.Hypertrophy;

    /// <summary>
    /// Validates a profile, returning the error to answer with or <c>null</c> when it is
    /// acceptable. Order matters only in that one error is returned at a time; the goal is
    /// checked first because a profile for an unsupported goal has no defensible bounds at all.
    /// </summary>
    public static ApiError? Validate(TrainingGoal goal, int daysPerWeek, int sessionDurationSeconds)
    {
        if (goal != SupportedGoal)
        {
            return new ApiError(TrainingErrorCodes.GoalNotSupported);
        }

        if (daysPerWeek < MinDaysPerWeek || daysPerWeek > MaxDaysPerWeek)
        {
            return new ApiError(TrainingErrorCodes.FrequencyOutOfRange, MinDaysPerWeek, MaxDaysPerWeek);
        }

        if (sessionDurationSeconds < MinSessionDurationSeconds
            || sessionDurationSeconds > MaxSessionDurationSeconds)
        {
            return new ApiError(
                TrainingErrorCodes.DurationOutOfRange,
                MinSessionDurationSeconds,
                MaxSessionDurationSeconds);
        }

        return null;
    }

    /// <summary>
    /// Parses a goal as the API receives it — case-insensitively, and without letting an
    /// unrecognised value become a deserialization failure. A client sending "powerlifting"
    /// gets <see cref="TrainingErrorCodes.GoalNotSupported"/>, which is a code the frontend can
    /// translate, rather than a framework error it cannot.
    /// </summary>
    public static bool TryParseGoal(string? goal, out TrainingGoal parsed) =>
        Enum.TryParse(goal, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);
}

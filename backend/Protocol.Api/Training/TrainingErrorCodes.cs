namespace Protocol.Api.Training;

/// <summary>
/// The stable, machine-readable codes the training endpoints answer with.
/// <para>
/// The backend never returns display text (root standard 3). The frontend owns every sentence
/// these become, in both locales — and because a translated string is not an identity, nothing
/// here may ever be branched on as prose.
/// </para>
/// </summary>
public static class TrainingErrorCodes
{
    /// <summary>Generating or reading with no profile saved.</summary>
    public const string ProfileNotFound = "ProfileNotFound";

    /// <summary>Any goal other than hypertrophy (ADR-004).</summary>
    public const string GoalNotSupported = "GoalNotSupported";

    /// <summary>Days per week outside the supported range (TD-002).</summary>
    public const string FrequencyOutOfRange = "FrequencyOutOfRange";

    /// <summary>Session duration outside the supported range (TD-012).</summary>
    public const string DurationOutOfRange = "DurationOutOfRange";
}

/// <summary>
/// An error, as the API returns it: a code plus whatever data the message needs.
/// <para>
/// <see cref="Min"/> and <see cref="Max"/> are carried so the frontend can say "between 2 and 6
/// days" without duplicating the bounds that TD-002 and TD-012 decided. A range copied into a
/// dictionary is a range that drifts when the record changes.
/// </para>
/// </summary>
public sealed record ApiError(string Code, int? Min = null, int? Max = null);

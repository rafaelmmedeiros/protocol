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

    /// <summary>An erase arrived without its confirmation. ADR-025 makes it deliberate or nothing.</summary>
    public const string EraseNotConfirmed = "EraseNotConfirmed";

    /// <summary>Any goal other than hypertrophy (ADR-004).</summary>
    public const string GoalNotSupported = "GoalNotSupported";

    /// <summary>Days per week outside the supported range (TD-002).</summary>
    public const string FrequencyOutOfRange = "FrequencyOutOfRange";

    /// <summary>Substituting a slot that is not in the current week.</summary>
    public const string PrescriptionNotFound = "PrescriptionNotFound";

    /// <summary>Excluding or preferring an exercise that is not ours.</summary>
    public const string ExerciseNotFound = "ExerciseNotFound";

    /// <summary>A preferred variant that does not belong to the pattern it is preferred for.</summary>
    public const string NotACandidate = "NotACandidate";

    /// <summary>A gym with nothing in it cannot be programmed for (ADR-013).</summary>
    public const string EquipmentSetEmpty = "EquipmentSetEmpty";

    /// <summary>An item outside the EquipmentItem vocabulary (ADR-013).</summary>
    public const string UnknownEquipmentItem = "UnknownEquipmentItem";

    /// <summary>Session duration outside the supported range (TD-012).</summary>
    public const string DurationOutOfRange = "DurationOutOfRange";

    /// <summary>
    /// Reading the current week before one has been generated. Not an error the user did
    /// anything to cause — the frontend turns this into the empty state, not a failure.
    /// </summary>
    public const string WeekNotFound = "WeekNotFound";
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

namespace Protocol.Api.Hevy;

/// <summary>
/// The stable codes the Hevy integration answers with. Codes, never display text
/// (root standard 3) — the frontend owns every sentence these become, in both locales.
/// </summary>
public static class HevyErrorCodes
{
    /// <summary>Hevy rejected the key when it was offered for saving.</summary>
    public const string HevyKeyInvalid = "HevyKeyInvalid";

    /// <summary>An operation needing a key ran for a user who has not connected one.</summary>
    public const string HevyNotConnected = "HevyNotConnected";

    /// <summary>Hevy could not be reached, or answered with a server error.</summary>
    public const string HevyUnreachable = "HevyUnreachable";

    /// <summary>
    /// Hevy refused for rate reasons. Reached only after the client has already retried with
    /// backoff (ADR-021) — this code means the retries were exhausted, not that one call failed.
    /// </summary>
    public const string HevyRateLimited = "HevyRateLimited";

    /// <summary>
    /// Hevy answered successfully and the body did not carry what we asked for, which means our
    /// reading of their shape is wrong. Never folded into <see cref="HevyUnreachable"/>: telling
    /// the user to try again would be a lie, because retrying cannot fix our own bug.
    /// </summary>
    public const string HevyUnreadable = "HevyUnreadable";

    /// <summary>A prescribed exercise has no external key and cannot be named to Hevy (ADR-016).</summary>
    public const string ExerciseNotMappable = "ExerciseNotMappable";

    /// <summary>A routine we meant to replace no longer exists in Hevy (ADR-017).</summary>
    public const string PushedRoutineMissing = "PushedRoutineMissing";

    /// <summary>
    /// The week's routines have already been trained from, so they are evidence and are not
    /// rewritten (ADR-017). Not a fault — regenerating produces a new week, which pushes freely.
    /// </summary>
    public const string WeekAlreadyTrainedFrom = "WeekAlreadyTrainedFrom";
}

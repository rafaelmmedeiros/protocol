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
}

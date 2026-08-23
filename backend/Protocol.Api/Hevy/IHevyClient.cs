namespace Protocol.Api.Hevy;

/// <summary>
/// Everything this system asks of Hevy, behind one seam.
/// <para>
/// The seam exists for two reasons. It keeps Hevy's shape out of the domain (root standard 17),
/// and it is what lets the suites run without ever reaching the real service — an integration
/// test that validated a key against api.hevyapp.com would be testing their uptime and spending
/// a real account's rate budget.
/// </para>
/// <para>
/// This interface grows one method at a time, as each step of M3 needs one. It currently holds
/// only what S3.1 requires.
/// </para>
/// </summary>
public interface IHevyClient
{
    /// <summary>
    /// Asks Hevy whether a key is real, by fetching the account it belongs to.
    /// <para>
    /// Validation happens before a key is stored (ADR-014) so that a typo fails at the moment
    /// the user can still fix it, rather than silently at the first sync.
    /// </para>
    /// </summary>
    Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token);
}

/// <summary>
/// The outcome of offering a key to Hevy. Three states, because "wrong key" and "Hevy is down"
/// are different answers to the user and must not collapse into one.
/// </summary>
public enum HevyKeyCheck
{
    /// <summary>Hevy accepted the key.</summary>
    Valid,

    /// <summary>Hevy answered, and rejected the key.</summary>
    Invalid,

    /// <summary>Hevy did not answer, or answered with a server error.</summary>
    Unreachable,
}

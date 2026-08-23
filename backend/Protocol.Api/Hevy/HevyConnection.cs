namespace Protocol.Api.Hevy;

/// <summary>
/// One user's connection to their own Hevy account (ADR-014).
/// <para>
/// The key belongs to the user, not to the deployment, so it is per-user data rather than
/// configuration — root standard 11 governs secrets *of the system*, and this is a secret *of
/// the user*. It is stored encrypted and is never returned by any endpoint, which is why there
/// is no read path for <see cref="ProtectedApiKey"/> anywhere above the protector.
/// </para>
/// </summary>
public sealed class HevyConnection
{
    public required Guid Id { get; init; }

    public required string UserId { get; init; }

    /// <summary>
    /// The user's Hevy key, protected at rest. Never leaves this type unprotected, and never
    /// reaches a response — see <c>HevyConnectionResponse</c>, which deliberately has no field
    /// for it.
    /// </summary>
    public required string ProtectedApiKey { get; set; }

    /// <summary>When the key was last saved and accepted by Hevy. UTC (root standard 5).</summary>
    public required DateTimeOffset ConnectedAt { get; set; }

    /// <summary>
    /// How far the import has read, as the events feed's <c>since</c> cursor (ADR-018). Null
    /// until the first sync; kept here rather than on the user because it is meaningless without
    /// a connection, and it dies with one.
    /// </summary>
    public DateTimeOffset? SyncCursor { get; set; }
}

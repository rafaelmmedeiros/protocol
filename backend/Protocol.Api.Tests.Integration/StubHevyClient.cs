using Protocol.Api.Hevy;

namespace Protocol.Api.Tests.Integration;

/// <summary>
/// Hevy, as the suite sees it.
/// <para>
/// Not a convenience. A test that reached api.hevyapp.com would depend on their uptime, spend a
/// real account's rate budget, and need a real credential in CI. <c>ApiFactory</c> registers this
/// for every integration test, which is what makes "no test run touches the real Hevy account"
/// true by construction rather than by everyone remembering.
/// </para>
/// <para>
/// It records what was written, because the push is only observable through what Hevy received —
/// asserting on our own stored identifiers would prove we saved something without proving we
/// sent the right thing.
/// </para>
/// </summary>
public sealed class StubHevyClient : IHevyClient
{
    private readonly Lock _gate = new();
    private long _nextFolderId = 1;
    private int _nextRoutine = 1;

    /// <summary>A key Hevy accepts.</summary>
    public const string ValidKey = "valid-0000-4a1b-9c3d-abcdefabcdef";

    /// <summary>A key Hevy answers about, and rejects.</summary>
    public const string InvalidKey = "wrong-0000-4a1b-9c3d-abcdefabcdef";

    /// <summary>A key Hevy never answers about at all.</summary>
    public const string UnreachableKey = "unreachable-4a1b-9c3d-abcdefabcd";

    /// <summary>Folders created, in order, by title.</summary>
    public List<string> FolderTitles { get; } = [];

    /// <summary>Routines created, in order: the identifier handed back, and what was sent.</summary>
    public List<(string RoutineId, HevyRoutinePayload Payload)> Created { get; } = [];

    /// <summary>Routines replaced, in order: which one, and what replaced it.</summary>
    public List<(string RoutineId, HevyRoutinePayload Payload)> Updated { get; } = [];

    /// <summary>
    /// Routine identifiers Hevy will claim not to have. Set by a test to reproduce the user
    /// deleting a pushed routine out from under us (ADR-017).
    /// </summary>
    public HashSet<string> Missing { get; } = [];

    public void Forget()
    {
        lock (_gate)
        {
            FolderTitles.Clear();
            Created.Clear();
            Updated.Clear();
            Missing.Clear();
        }
    }

    public Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token) =>
        Task.FromResult(apiKey switch
        {
            ValidKey => HevyKeyCheck.Valid,
            UnreachableKey => HevyKeyCheck.Unreachable,
            _ => HevyKeyCheck.Invalid,
        });

    public Task<HevyWrite<long>> CreateFolderAsync(string apiKey, string title, CancellationToken token)
    {
        lock (_gate)
        {
            FolderTitles.Add(title);
            return Task.FromResult(new HevyWrite<long>(HevyWriteOutcome.Ok, _nextFolderId++));
        }
    }

    public Task<HevyWrite<string>> CreateRoutineAsync(
        string apiKey,
        HevyRoutinePayload routine,
        CancellationToken token)
    {
        lock (_gate)
        {
            // A uuid-shaped identifier, like the real one, so nothing downstream can accidentally
            // depend on it being short or numeric.
            var id = $"7b2281f1-0000-4000-8000-{_nextRoutine++:D12}";
            Created.Add((id, routine));
            return Task.FromResult(new HevyWrite<string>(HevyWriteOutcome.Ok, id));
        }
    }

    public Task<HevyWrite<string>> UpdateRoutineAsync(
        string apiKey,
        string routineId,
        HevyRoutinePayload routine,
        CancellationToken token)
    {
        lock (_gate)
        {
            if (Missing.Contains(routineId))
            {
                return Task.FromResult(new HevyWrite<string>(HevyWriteOutcome.NotFound));
            }

            Updated.Add((routineId, routine));
            return Task.FromResult(new HevyWrite<string>(HevyWriteOutcome.Ok, routineId));
        }
    }
}

using System.Collections.Concurrent;

namespace Protocol.Api.Hevy;

/// <summary>
/// Hevy, simulated in memory. **Only ever registered when <c>Hevy:UseFake</c> is set**, which
/// only <c>docker-compose.test.yml</c> does.
/// <para>
/// It exists because the end-to-end suite runs the real API container, and without this the
/// browser test would reach <c>api.hevyapp.com</c> — depending on a third party's uptime,
/// spending a real account's rate budget, and needing a real credential in CI. The unit and
/// integration suites substitute <see cref="IHevyClient"/> in process; a container cannot, so the
/// switch has to live here.
/// </para>
/// <para>
/// It is deliberately not a null object. A fake that accepted writes and returned nothing to read
/// would let the loop's most important test — pushed, trained, synced, compared — pass without
/// the loop ever closing. So it **synthesises a workout from each routine it was given**, with
/// repetitions falling across the sets the way a lifter terminating on effort actually logs them.
/// </para>
/// </summary>
public sealed class FakeHevyClient : IHevyClient
{
    /// <summary>The configuration key. Absent everywhere except the test stack.</summary>
    public const string EnabledKey = "Hevy:UseFake";

    /// <summary>The one key this fake rejects, so the failure path is reachable from a browser.</summary>
    public const string RejectedKey = "invalid-key";

    /// <summary>
    /// Keyed by api key as well as routine, so one account never sees another's routines. The
    /// real service is per-account and a fake that is not would let a suite pass on behaviour
    /// the product does not have.
    /// </summary>
    private readonly ConcurrentDictionary<(string ApiKey, string RoutineId), HevyRoutinePayload> _routines = new();
    private long _nextFolderId;
    private int _nextRoutine;

    public Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token) =>
        Task.FromResult(apiKey == RejectedKey ? HevyKeyCheck.Invalid : HevyKeyCheck.Valid);

    public Task<HevyWrite<long>> CreateFolderAsync(string apiKey, string title, CancellationToken token) =>
        Task.FromResult(new HevyWrite<long>(HevyWriteOutcome.Ok, Interlocked.Increment(ref _nextFolderId)));

    public Task<HevyWrite<bool>> FolderExistsAsync(string apiKey, long folderId, CancellationToken token) =>
        Task.FromResult(new HevyWrite<bool>(HevyWriteOutcome.Ok, true));

    public Task<HevyWrite<string>> CreateRoutineAsync(
        string apiKey,
        HevyRoutinePayload routine,
        CancellationToken token)
    {
        var id = $"7b2281f1-0000-4000-8000-{Interlocked.Increment(ref _nextRoutine):D12}";
        _routines[(apiKey, id)] = routine;

        return Task.FromResult(new HevyWrite<string>(HevyWriteOutcome.Ok, id));
    }

    public Task<HevyWrite<string>> UpdateRoutineAsync(
        string apiKey,
        string routineId,
        HevyRoutinePayload routine,
        CancellationToken token)
    {
        _routines[(apiKey, routineId)] = routine;

        return Task.FromResult(new HevyWrite<string>(HevyWriteOutcome.Ok, routineId));
    }

    public Task<HevyWrite<HevyWorkoutEventPage>> ListWorkoutEventsAsync(
        string apiKey,
        DateTimeOffset since,
        int page,
        int pageSize,
        CancellationToken token)
    {
        // One workout per routine that was pushed, as if the user had trained every session. The
        // timestamps are derived from the routine's position rather than from the clock, so the
        // suite is deterministic and a re-sync recognises what it already read.
        var events = _routines
            .Where(entry => entry.Key.ApiKey == apiKey)
            .OrderBy(entry => entry.Key.RoutineId, StringComparer.Ordinal)
            .Select((entry, index) => Trained(entry.Key.RoutineId, entry.Value, index))
            .Where(change => change.Workout!.UpdatedAt >= since)
            .ToList();

        var slice = events.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var pageCount = Math.Max(1, (int)Math.Ceiling(events.Count / (double)pageSize));

        return Task.FromResult(new HevyWrite<HevyWorkoutEventPage>(
            HevyWriteOutcome.Ok,
            new HevyWorkoutEventPage(page, pageCount, slice)));
    }

    private static HevyWorkoutEvent Trained(string routineId, HevyRoutinePayload routine, int index)
    {
        var at = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddDays(index);

        var exercises = routine.Exercises
            .Select((exercise, position) => new HevyWorkoutExercise(
                position,
                exercise.ExerciseTemplateId,
                null,
                exercise.ExerciseTemplateId,
                null,
                [.. exercise.Sets.Select((set, setIndex) => new HevyWorkoutSet(
                    setIndex,
                    "normal",
                    50,
                    // Falling across the sets: the top of the prescribed range, then one fewer
                    // each time. Effort held constant while repetitions decline is what a lifter
                    // terminating on effort produces, and it is what the comparison must show.
                    Math.Max(set.RepRange?.Start ?? 8, (set.RepRange?.End ?? 12) - setIndex),
                    null,
                    null,
                    null,
                    null))]))
            .ToList();

        return new HevyWorkoutEvent(
            "updated",
            new HevyWorkout(
                $"fake-workout-{routineId}",
                routine.Title,
                routineId,
                null,
                at,
                at.AddHours(1),
                at.AddHours(1),
                at,
                exercises),
            null,
            null);
    }
}

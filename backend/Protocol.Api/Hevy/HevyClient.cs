using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace Protocol.Api.Hevy;

/// <summary>
/// The one place this system talks to Hevy over HTTP (root standard 17).
/// <para>
/// Sequential and retrying, because Hevy declares no rate limit at all — its OpenAPI document
/// has no 429, no <c>Retry-After</c> and no rate-limit header — so the client is written to
/// survive a limit it cannot know rather than to a number we would be guessing (ADR-021).
/// </para>
/// </summary>
public sealed class HevyClient(HttpClient http, ILogger<HevyClient> logger, IHevyBackoff backoff)
    : IHevyClient
{
    /// <summary>Hevy authenticates with a bare header, not a bearer token.</summary>
    private const string KeyHeader = "api-key";

    /// <summary>
    /// Attempts, not retries — three in total. Small on purpose: the only caller today is a user
    /// waiting on a save, and a long backoff there reads as a hung screen.
    /// </summary>
    private const int MaxAttempts = 3;

    public async Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token)
    {
        var response = await SendWithBackoffAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "user/info");
                // Set per request rather than on the shared HttpClient: the client is reused
                // across every user, and a default header would leak one user's key into
                // another user's call.
                request.Headers.Add(KeyHeader, apiKey);
                return request;
            },
            token);

        if (response is null)
        {
            return HevyKeyCheck.Unreachable;
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return HevyKeyCheck.Valid;
            }

            // Hevy answered and refused. That is a fact about the key, not about the service,
            // and the two must not collapse — the user can fix one and not the other.
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return HevyKeyCheck.Invalid;
            }

            return HevyKeyCheck.Unreachable;
        }
    }

    public async Task<HevyWrite<long>> CreateFolderAsync(
        string apiKey,
        string title,
        CancellationToken token) =>
        await WriteAsync<HevyFolderEnvelope, long>(
            apiKey,
            HttpMethod.Post,
            "routine_folders",
            new HevyFolderRequest(new HevyFolderPayload(title)),
            envelope => envelope.RoutineFolder?.Id ?? 0,
            token);

    public async Task<HevyWrite<bool>> FolderExistsAsync(
        string apiKey,
        long folderId,
        CancellationToken token)
    {
        var response = await SendWithBackoffAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"routine_folders/{folderId}");
                request.Headers.Add(KeyHeader, apiKey);
                return request;
            },
            token);

        if (response is null)
        {
            return new HevyWrite<bool>(HevyWriteOutcome.Unreachable);
        }

        using (response)
        {
            // Status only. The body of a 404 here is plain prose, and nothing should read it.
            return response.StatusCode switch
            {
                HttpStatusCode.OK => new HevyWrite<bool>(HevyWriteOutcome.Ok, true),
                HttpStatusCode.NotFound => new HevyWrite<bool>(HevyWriteOutcome.NotFound),
                HttpStatusCode.TooManyRequests => new HevyWrite<bool>(HevyWriteOutcome.RateLimited),
                _ => new HevyWrite<bool>(HevyWriteOutcome.Unreachable),
            };
        }
    }

    public async Task<HevyWrite<string>> CreateRoutineAsync(
        string apiKey,
        HevyRoutinePayload routine,
        CancellationToken token) =>
        await WriteAsync<HevyRoutineEnvelope, string>(
            apiKey,
            HttpMethod.Post,
            "routines",
            new HevyRoutineRequest(routine),
            envelope => envelope.Routine?.FirstOrDefault()?.Id,
            token);

    public async Task<HevyWrite<string>> UpdateRoutineAsync(
        string apiKey,
        string routineId,
        HevyRoutinePayload routine,
        CancellationToken token) =>
        await WriteAsync<HevyRoutineEnvelope, string>(
            apiKey,
            HttpMethod.Put,
            $"routines/{Uri.EscapeDataString(routineId)}",
            new HevyRoutineRequest(routine),
            // The identifier we asked for, when the answer does not repeat it. A PUT names its
            // target in the path, so losing it here would be losing the join for no reason.
            envelope => envelope.Routine?.FirstOrDefault()?.Id ?? routineId,
            token);

    public async Task<HevyWrite<HevyWorkoutEventPage>> ListWorkoutEventsAsync(
        string apiKey,
        DateTimeOffset since,
        int page,
        int pageSize,
        CancellationToken token)
    {
        // Round-trip format, so the cursor survives the wire exactly as stored. A local-time
        // string here would shift the window by the server's offset and silently re-read or skip.
        var query = $"workouts/events?since={Uri.EscapeDataString(since.UtcDateTime.ToString("O"))}"
            + $"&page={page}&pageSize={pageSize}";

        var response = await SendWithBackoffAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, query);
                request.Headers.Add(KeyHeader, apiKey);
                return request;
            },
            token);

        if (response is null)
        {
            return new HevyWrite<HevyWorkoutEventPage>(HevyWriteOutcome.Unreachable);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return new HevyWrite<HevyWorkoutEventPage>(
                    response.StatusCode == HttpStatusCode.TooManyRequests
                        ? HevyWriteOutcome.RateLimited
                        : HevyWriteOutcome.Unreachable);
            }

            var payload = await response.Content.ReadFromJsonAsync<HevyWorkoutEventPage>(token);

            return payload is null
                ? new HevyWrite<HevyWorkoutEventPage>(HevyWriteOutcome.Unreachable)
                : new HevyWrite<HevyWorkoutEventPage>(HevyWriteOutcome.Ok, payload);
        }
    }

    /// <summary>
    /// One shape for every write: send, retry per ADR-021, then turn the answer into our own
    /// vocabulary. Nothing above this method sees a status code.
    /// </summary>
    private async Task<HevyWrite<TValue>> WriteAsync<TResponse, TValue>(
        string apiKey,
        HttpMethod method,
        string path,
        object body,
        Func<TResponse, TValue?> select,
        CancellationToken token)
    {
        var response = await SendWithBackoffAsync(
            () =>
            {
                var request = new HttpRequestMessage(method, path)
                {
                    Content = JsonContent.Create(body, options: null),
                };
                request.Headers.Add(KeyHeader, apiKey);
                return request;
            },
            token);

        if (response is null)
        {
            return new HevyWrite<TValue>(HevyWriteOutcome.Unreachable);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<TResponse>(token);
                var value = payload is null ? default : select(payload);

                // A success whose body we could not read is not a success. Treating it as one is
                // how a folder identifier of zero got stored and then rejected by every routine
                // sent into it -- the shape was wrong and nothing said so.
                if (payload is null || value is null || value.Equals(default(TValue)))
                {
                    logger.LogWarning(
                        "Hevy answered {Status} to {Path} with a body we could not read",
                        (int)response.StatusCode,
                        path);

                    return new HevyWrite<TValue>(HevyWriteOutcome.Unreadable);
                }

                return new HevyWrite<TValue>(HevyWriteOutcome.Ok, value);
            }

            // The reason, logged rather than discarded. Hevy answers a 400 with an `error`
            // string, and throwing it away turns a fixable mistake into a mystery.
            var reason = await response.Content.ReadAsStringAsync(token);

            logger.LogWarning(
                "Hevy refused {Path} with {Status}: {Reason}",
                path,
                (int)response.StatusCode,
                reason.Length > 500 ? reason[..500] : reason);

            return new HevyWrite<TValue>(response.StatusCode switch
            {
                // The routine we meant to replace is gone -- the user deleted it in Hevy. A fact
                // about their account, not a fault of ours (ADR-017).
                HttpStatusCode.NotFound => HevyWriteOutcome.NotFound,
                HttpStatusCode.TooManyRequests => HevyWriteOutcome.RateLimited,
                _ => HevyWriteOutcome.Unreachable,
            });
        }
    }

    /// <summary>
    /// Sends, retrying a refusal or a server error with backoff. Returns null when every attempt
    /// failed, which the caller turns into its own vocabulary.
    /// <para>
    /// The request is built by a factory rather than passed in, because an
    /// <see cref="HttpRequestMessage"/> cannot be sent twice.
    /// </para>
    /// </summary>
    private async Task<HttpResponseMessage?> SendWithBackoffAsync(
        Func<HttpRequestMessage> newRequest,
        CancellationToken token)
    {
        // Every call is logged under the ambient trace, so a request is followable from the
        // browser through the API and out to Hevy (root standard 12). The identifier is the one
        // ASP.NET Core already creates and HttpClient already propagates as traceparent -- there
        // is nothing to invent, and inventing a second one would give a request two names.
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.TraceId.ToString(),
        });

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            HttpResponseMessage response;

            try
            {
                var request = newRequest();

                // The method and path, never the header. The key is the one thing in this client
                // that must not reach a log, and the request is logged before it is sent so a
                // call that hangs still leaves a trace.
                logger.LogInformation(
                    "Hevy {Method} {Path} attempt {Attempt}",
                    request.Method,
                    request.RequestUri,
                    attempt);

                response = await http.SendAsync(request, token);
            }
            catch (HttpRequestException exception)
            {
                // No key is ever logged, here or anywhere else in this client.
                logger.LogWarning(exception, "Hevy request failed on attempt {Attempt}", attempt);

                if (attempt == MaxAttempts)
                {
                    return null;
                }

                await backoff.WaitAsync(attempt, null, token);
                continue;
            }

            var status = response.StatusCode;
            var retryable = status == HttpStatusCode.TooManyRequests || (int)status >= 500;

            if (!retryable || attempt == MaxAttempts)
            {
                return response;
            }

            // Honoured if it ever appears. The published contract does not include it, so this
            // is a courtesy the code is ready for rather than one it depends on.
            var retryAfter = response.Headers.RetryAfter?.Delta;
            response.Dispose();

            logger.LogWarning(
                "Hevy answered {Status} on attempt {Attempt}; backing off",
                (int)status,
                attempt);

            await backoff.WaitAsync(attempt, retryAfter, token);
        }

        return null;
    }
}

/// <summary>
/// How long to wait between attempts. A seam rather than a bare <c>Task.Delay</c>, so a suite
/// exercising the retry path does not pay real seconds for it.
/// </summary>
public interface IHevyBackoff
{
    Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken token);
}

/// <summary>Exponential backoff: one second, then two, then four.</summary>
public sealed class ExponentialHevyBackoff : IHevyBackoff
{
    public Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken token) =>
        Task.Delay(retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), token);
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;

namespace Protocol.Api.Training;

/// <summary>
/// The training profile: what the user trains for and what they have available.
/// <para>
/// Every response is codes and data, never display text (root standard 3). JSON is camelCase,
/// which is the ASP.NET Core default the frontend depends on.
/// </para>
/// </summary>
public static class TrainingEndpoints
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/training").WithTags("Training").RequireAuthorization();

        group.MapGet("/profile", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var profile = await db.TrainingProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.UserId == UserIdOf(user), token);

                return profile is null
                    ? Results.NotFound(new ApiError(TrainingErrorCodes.ProfileNotFound))
                    : Results.Ok(TrainingProfileResponse.From(profile));
            })
            .WithName("GetTrainingProfile");

        group.MapPut("/profile", async (
                TrainingProfileRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                if (!TrainingProfileRules.TryParseGoal(request.Goal, out var goal))
                {
                    // An unrecognised goal and a recognised-but-unsupported one are the same
                    // answer to the caller: this product does not programme for it yet.
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.GoalNotSupported));
                }

                var error = TrainingProfileRules.Validate(
                    goal, request.DaysPerWeek, request.SessionDurationSeconds);

                if (error is not null)
                {
                    return Results.BadRequest(error);
                }

                var userId = UserIdOf(user);
                var profile = await db.TrainingProfiles.SingleOrDefaultAsync(p => p.UserId == userId, token);

                if (profile is null)
                {
                    profile = new TrainingProfile
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = userId,
                        Goal = goal,
                        DaysPerWeek = request.DaysPerWeek,
                        SessionDurationSeconds = request.SessionDurationSeconds,
                    };
                    db.TrainingProfiles.Add(profile);
                }
                else
                {
                    // A profile is current state, not history: replacing it is correct here.
                    // What must never change is a week already generated from it, which is why
                    // ADR-003 snapshots these values onto the week rather than referencing them.
                    profile.Goal = goal;
                    profile.DaysPerWeek = request.DaysPerWeek;
                    profile.SessionDurationSeconds = request.SessionDurationSeconds;
                }

                await db.SaveChangesAsync(token);
                return Results.Ok(TrainingProfileResponse.From(profile));
            })
            .WithName("PutTrainingProfile");

        group.MapGet("/equipment", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var owned = await OwnedItemsAsync(db, UserIdOf(user), token);

                return Results.Ok(new EquipmentResponse(
                    [.. owned.Select(item => item.ToString()).Order()],
                    [.. Enum.GetValues<EquipmentItem>().Select(item => item.ToString())]));
            })
            .WithName("GetEquipment");

        group.MapPut("/equipment", async (
                EquipmentRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var parsed = new List<EquipmentItem>();
                foreach (var name in request.Items ?? [])
                {
                    if (!Enum.TryParse<EquipmentItem>(name, ignoreCase: true, out var item)
                        || !Enum.IsDefined(item))
                    {
                        // Parsed here rather than bound to the enum, so an unknown value becomes
                        // a code the frontend can translate instead of a framework error it
                        // cannot (root standard 3).
                        return Results.BadRequest(new ApiError(TrainingErrorCodes.UnknownEquipmentItem));
                    }

                    parsed.Add(item);
                }

                if (parsed.Count == 0)
                {
                    // A gym with nothing in it cannot be programmed for, and an empty set would
                    // otherwise be indistinguishable from "never described", which means the
                    // TD-004 default -- the opposite of what the user just said.
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.EquipmentSetEmpty));
                }

                var userId = UserIdOf(user);
                var current = await db.UserEquipment.Where(row => row.UserId == userId).ToListAsync(token);

                db.UserEquipment.RemoveRange(current);
                db.UserEquipment.AddRange(parsed.Distinct().Select(item => new UserEquipment
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    Item = item,
                }));

                await db.SaveChangesAsync(token);

                return Results.Ok(new EquipmentResponse(
                    [.. parsed.Distinct().Select(item => item.ToString()).Order()],
                    [.. Enum.GetValues<EquipmentItem>().Select(item => item.ToString())]));
            })
            .WithName("PutEquipment");

        group.MapGet("/preferences", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var userId = UserIdOf(user);

                var excluded = await db.ExerciseExclusions
                    .AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => row.ExerciseId)
                    .ToListAsync(token);

                var preferred = await db.PreferredVariants
                    .AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => new PreferredVariantResponse(row.MovementPattern.ToString(), row.ExerciseId))
                    .ToListAsync(token);

                return Results.Ok(new PreferencesResponse(excluded, preferred));
            })
            .WithName("GetPreferences");

        group.MapPut("/preferences", async (
                PreferencesRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var userId = UserIdOf(user);
                var excluded = (request.ExcludedExerciseIds ?? []).Distinct().ToList();
                var preferred = request.PreferredVariants ?? [];

                var known = await db.Exercises
                    .AsNoTracking()
                    .Select(exercise => new { exercise.Id, exercise.MovementPattern })
                    .ToDictionaryAsync(exercise => exercise.Id, exercise => exercise.MovementPattern, token);

                if (excluded.Any(id => !known.ContainsKey(id))
                    || preferred.Any(row => !known.ContainsKey(row.ExerciseId)))
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.ExerciseNotFound));
                }

                // A preferred variant has to belong to the pattern it is preferred for, or the
                // preference could never fire and would look like the generator ignoring it.
                if (preferred.Any(row =>
                        !Enum.TryParse<MovementPattern>(row.MovementPattern, ignoreCase: true, out var pattern)
                        || !Enum.IsDefined(pattern)
                        || known[row.ExerciseId] != pattern))
                {
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.NotACandidate));
                }

                db.ExerciseExclusions.RemoveRange(
                    await db.ExerciseExclusions.Where(row => row.UserId == userId).ToListAsync(token));
                db.PreferredVariants.RemoveRange(
                    await db.PreferredVariants.Where(row => row.UserId == userId).ToListAsync(token));

                db.ExerciseExclusions.AddRange(excluded.Select(id => new ExerciseExclusion
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    ExerciseId = id,
                }));

                db.PreferredVariants.AddRange(preferred.Select(row => new PreferredVariant
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    MovementPattern = known[row.ExerciseId],
                    ExerciseId = row.ExerciseId,
                }));

                await db.SaveChangesAsync(token);

                return Results.Ok(new PreferencesResponse(
                    excluded,
                    [.. preferred.Select(row => new PreferredVariantResponse(
                        known[row.ExerciseId].ToString(),
                        row.ExerciseId))]));
            })
            .WithName("PutPreferences");

        group.MapPost("/weeks", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var userId = UserIdOf(user);
                var profile = await db.TrainingProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.UserId == userId, token);

                if (profile is null)
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.ProfileNotFound));
                }

                var catalogue = await db.Exercises
                    .Include(exercise => exercise.Muscles)
                    .Include(exercise => exercise.Requirements)
                    .AsNoTracking()
                    .ToListAsync(token);

                var owned = await OwnedItemsAsync(db, userId, token);
                var preferences = await PreferencesOf(db, userId, token);

                // Truncated to the microsecond because that is all Postgres `timestamptz`
                // keeps, while a DateTimeOffset holds 100-nanosecond ticks. Without this the
                // value returned by this endpoint and the value read back a moment later differ
                // in their last digit -- equal to any human, unequal to any comparison.
                var now = DateTimeOffset.UtcNow;
                var generatedAt = new DateTimeOffset(now.Ticks - (now.Ticks % 10), now.Offset);
                var plan = WeekGenerator.Generate(
                    profile,
                    catalogue,
                    DateOnly.FromDateTime(generatedAt.UtcDateTime),
                    owned,
                    preferences);

                var current = await db.GeneratedWeeks
                    .AsNoTracking()
                    .Include(w => w.Sessions).ThenInclude(s => s.Prescriptions)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.GeneratedAt)
                    .FirstOrDefaultAsync(token);

                // Regenerating never edits what is stored (ADR-003) -- but it also does not write
                // a week identical to the one already there. The generator is deterministic
                // (ADR-005), so an unchanged profile can only reproduce what exists, and an
                // identical row is not a discarded alternative: it is the same answer written
                // twice, carrying none of the explanatory value that justified storing weeks at
                // all (ADR-009).
                if (current is not null && Matches(current, plan))
                {
                    return Results.Ok(GeneratedWeekResponse.From(current, catalogue));
                }

                var week = Persist(plan, profile, userId, generatedAt);
                db.GeneratedWeeks.Add(week);
                await db.SaveChangesAsync(token);

                return Results.Ok(GeneratedWeekResponse.From(week, catalogue));
            })
            .WithName("GenerateTrainingWeek");

        group.MapGet("/weeks/current", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var userId = UserIdOf(user);

                var week = await db.GeneratedWeeks
                    .AsNoTracking()
                    .Include(w => w.Sessions).ThenInclude(s => s.Prescriptions)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.GeneratedAt)
                    .FirstOrDefaultAsync(token);

                if (week is null)
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.WeekNotFound));
                }

                var catalogue = await db.Exercises.AsNoTracking().ToListAsync(token);
                return Results.Ok(GeneratedWeekResponse.From(week, catalogue));
            })
            .WithName("GetCurrentTrainingWeek");

        return app;
    }

    /// <summary>
    /// Whether a freshly generated plan is the week already stored.
    /// <para>
    /// Compares everything a week asserts — the Monday it starts on, its sessions, and every
    /// number on every slot. When in doubt the safe direction is to report <c>false</c> and
    /// write: a duplicate row is noise, and a skipped write that should have happened is a lost
    /// week (ADR-009).
    /// </para>
    /// </summary>
    private static bool Matches(GeneratedWeek stored, WeekPlan plan)
    {
        if (stored.WeekStartDate != plan.WeekStartDate) return false;

        var storedSessions = stored.Sessions.OrderBy(session => session.Position).ToList();
        if (storedSessions.Count != plan.Sessions.Count) return false;

        return storedSessions.Zip(plan.Sessions).All(pair =>
        {
            var (left, right) = pair;
            if (left.Day != right.Day || left.Kind != right.Kind) return false;

            var storedSlots = left.Prescriptions.OrderBy(slot => slot.Position).ToList();
            if (storedSlots.Count != right.Slots.Count) return false;

            return storedSlots.Zip(right.Slots).All(slots =>
            {
                var (a, b) = slots;
                return a.Position == b.Position
                    && a.ExerciseId == b.Exercise.Id
                    && a.Sets == b.Sets
                    && a.MinReps == b.Prescription.MinReps
                    && a.MaxReps == b.Prescription.MaxReps
                    && a.RepsInReserve == b.Prescription.RepsInReserve
                    && a.RestSeconds == b.Prescription.RestSeconds;
            });
        });
    }

    /// <summary>
    /// Maps a generated plan onto the entities that store it, snapshotting the profile's values
    /// onto the week rather than referencing the profile (ADR-003).
    /// </summary>
    private static GeneratedWeek Persist(
        WeekPlan plan,
        TrainingProfile profile,
        string userId,
        DateTimeOffset generatedAt) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            WeekStartDate = plan.WeekStartDate,
            GeneratedAt = generatedAt,
            Goal = profile.Goal,
            DaysPerWeek = profile.DaysPerWeek,
            SessionDurationSeconds = profile.SessionDurationSeconds,
            Sessions =
            [
                .. plan.Sessions.Select(session => new GeneratedSession
                {
                    Id = Guid.CreateVersion7(),
                    Position = session.Position,
                    Day = session.Day,
                    Kind = session.Kind,
                    Prescriptions =
                    [
                        .. session.Slots.Select(slot => new GeneratedPrescription
                        {
                            Id = Guid.CreateVersion7(),
                            Position = slot.Position,
                            ExerciseId = slot.Exercise.Id,
                            Sets = slot.Sets,
                            MinReps = slot.Prescription.MinReps,
                            MaxReps = slot.Prescription.MaxReps,
                            RepsInReserve = slot.Prescription.RepsInReserve,
                            RestSeconds = slot.Prescription.RestSeconds,
                        }),
                    ],
                }),
            ],
        };

    /// <summary>
    /// What the user owns, or `TD-004`'s assumed gym when they have never said. No rows is not
    /// an empty gym — the endpoint refuses an empty set precisely so the two cannot be confused
    /// (ADR-013).
    /// </summary>
    /// <summary>
    /// What the user has said about exercises, in the shape the generator consumes. Absent rows
    /// mean no preference, which is a different thing from an empty gym — nothing here has a
    /// default to fall back to.
    /// </summary>
    private static async Task<TrainingPreferences> PreferencesOf(
        AppDbContext db,
        string userId,
        CancellationToken token)
    {
        var excluded = await db.ExerciseExclusions
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.ExerciseId)
            .ToListAsync(token);

        var preferred = await db.PreferredVariants
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .ToListAsync(token);

        return new TrainingPreferences(
            excluded.ToHashSet(),
            preferred.ToDictionary(row => row.MovementPattern, row => row.ExerciseId));
    }

    private static async Task<IReadOnlySet<EquipmentItem>> OwnedItemsAsync(
        AppDbContext db,
        string userId,
        CancellationToken token)
    {
        var rows = await db.UserEquipment
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.Item)
            .ToListAsync(token);

        return rows.Count == 0 ? ExerciseCatalogue.AssumedGym : rows.ToHashSet();
    }

    private static string UserIdOf(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated principal has no identifier.");
}

/// <summary>
/// A profile as the client sends it. The goal arrives as a string rather than an enum so that
/// an unrecognised value becomes <see cref="TrainingErrorCodes.GoalNotSupported"/> — a code the
/// frontend can translate — instead of a deserialization failure it cannot.
/// </summary>
public sealed record TrainingProfileRequest(string? Goal, int DaysPerWeek, int SessionDurationSeconds);

/// <summary>
/// A profile as the API returns it. Duration is seconds, always: minutes are a rendering
/// concern and are converted at the edge (root standard 4).
/// </summary>
public sealed record TrainingProfileResponse(string Goal, int DaysPerWeek, int SessionDurationSeconds)
{
    public static TrainingProfileResponse From(TrainingProfile profile) =>
        new(profile.Goal.ToString(), profile.DaysPerWeek, profile.SessionDurationSeconds);
}

/// <summary>
/// A stored week as the API returns it. Every enum travels as its stable name, never as a
/// sentence — the frontend owns the words and both locales (root standard 3).
/// </summary>
public sealed record GeneratedWeekResponse(
    Guid Id,
    DateOnly WeekStartDate,
    DateTimeOffset GeneratedAt,
    string Goal,
    int DaysPerWeek,
    int SessionDurationSeconds,
    int EstimatedSeconds,
    IReadOnlyList<GeneratedSessionResponse> Sessions)
{
    /// <summary>
    /// A session's expected duration: its warm-up, plus what each slot costs in sets, rest and
    /// the transition to the next exercise (`TD-012`). Computed from the prescriptions actually
    /// stored, never from a column.
    /// </summary>
    internal static int EstimatedSecondsFor(GeneratedSession session) =>
        SessionTimeBudget.WarmUpSeconds // TD-012
        + session.Prescriptions.Sum(prescription => SessionTimeBudget.SlotCostSeconds(
            prescription.MinReps,
            prescription.MaxReps,
            prescription.Sets,
            prescription.RestSeconds));

    public static GeneratedWeekResponse From(GeneratedWeek week, IReadOnlyList<Exercise> catalogue)
    {
        var titles = catalogue.ToDictionary(exercise => exercise.Id);

        return new GeneratedWeekResponse(
            week.Id,
            week.WeekStartDate,
            week.GeneratedAt,
            week.Goal.ToString(),
            week.DaysPerWeek,
            week.SessionDurationSeconds,
            week.Sessions.Sum(EstimatedSecondsFor),
            [
                .. week.Sessions
                    .OrderBy(session => session.Position)
                    .Select(session => new GeneratedSessionResponse(
                        session.Position,
                        session.Day.ToString(),
                        session.Kind.ToString(),
                        EstimatedSecondsFor(session),
                        [
                            .. session.Prescriptions
                                .OrderBy(prescription => prescription.Position)
                                .Select(prescription => GeneratedPrescriptionResponse.From(
                                    prescription,
                                    titles.GetValueOrDefault(prescription.ExerciseId))),
                        ])),
            ]);
    }
}

/// <summary>One day of a stored week.</summary>
/// <param name="EstimatedSeconds">
/// How long this session is expected to take, computed on read and **never stored**. It is
/// derivable from the prescriptions beside it, and a derived column can disagree with its own
/// source — the reasoning `S1.9` used to decline `cut_applied`.
/// <para>
/// Two of the terms behind it — the transition between exercises and the warm-up — are
/// engineering estimates with no source (`TD-012`). Showing the number is what makes them
/// falsifiable: a session that reads 52 minutes and takes 70 says the constants are wrong.
/// </para>
/// </param>
public sealed record GeneratedSessionResponse(
    int Position,
    string Day,
    string Kind,
    int EstimatedSeconds,
    IReadOnlyList<GeneratedPrescriptionResponse> Prescriptions);

/// <summary>
/// One slot. The title travels for display only and is never something to match on (root
/// standard 9); <c>externalTemplateId</c> is what resolves the exercise in Hevy.
/// </summary>
public sealed record GeneratedPrescriptionResponse(
    int Position,
    Guid ExerciseId,
    string ExerciseTitle,
    string ExternalTemplateId,
    int Sets,
    int MinReps,
    int MaxReps,
    int RepsInReserve,
    int RestSeconds)
{
    public static GeneratedPrescriptionResponse From(GeneratedPrescription prescription, Exercise? exercise) =>
        new(
            prescription.Position,
            prescription.ExerciseId,
            exercise?.Title ?? string.Empty,
            exercise?.ExternalTemplateId ?? string.Empty,
            prescription.Sets,
            prescription.MinReps,
            prescription.MaxReps,
            prescription.RepsInReserve,
            prescription.RestSeconds);
}

/// <summary>
/// What the user has, and everything they could say they have. The vocabulary travels with the
/// answer so the screen never hardcodes a list that would drift from the enum.
/// </summary>
public sealed record EquipmentResponse(IReadOnlyList<string> Items, IReadOnlyList<string> Vocabulary);

/// <summary>
/// Items arrive as strings rather than bound to the enum, so an unrecognised one becomes
/// <see cref="TrainingErrorCodes.UnknownEquipmentItem"/> — a code the frontend can translate —
/// instead of a deserialization failure it cannot.
/// </summary>
public sealed record EquipmentRequest(IReadOnlyList<string>? Items);

/// <summary>
/// What the user has said about exercises. Two lists and no scores — a blended rank would let an
/// invented weight override a real preference (`ADR-011`, `TD-016`).
/// </summary>
public sealed record PreferencesResponse(
    IReadOnlyList<Guid> ExcludedExerciseIds,
    IReadOnlyList<PreferredVariantResponse> PreferredVariants);

/// <summary>The exercise chosen for a movement pattern, whenever that pattern is needed.</summary>
public sealed record PreferredVariantResponse(string MovementPattern, Guid ExerciseId);

public sealed record PreferencesRequest(
    IReadOnlyList<Guid>? ExcludedExerciseIds,
    IReadOnlyList<PreferredVariantRequest>? PreferredVariants);

public sealed record PreferredVariantRequest(string MovementPattern, Guid ExerciseId);

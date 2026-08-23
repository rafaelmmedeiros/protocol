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
                    .AsNoTracking()
                    .ToListAsync(token);

                // Truncated to the microsecond because that is all Postgres `timestamptz`
                // keeps, while a DateTimeOffset holds 100-nanosecond ticks. Without this the
                // value returned by this endpoint and the value read back a moment later differ
                // in their last digit -- equal to any human, unequal to any comparison.
                var now = DateTimeOffset.UtcNow;
                var generatedAt = new DateTimeOffset(now.Ticks - (now.Ticks % 10), now.Offset);
                var plan = WeekGenerator.Generate(
                    profile,
                    catalogue,
                    DateOnly.FromDateTime(generatedAt.UtcDateTime));

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
    IReadOnlyList<GeneratedSessionResponse> Sessions)
{
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
            [
                .. week.Sessions
                    .OrderBy(session => session.Position)
                    .Select(session => new GeneratedSessionResponse(
                        session.Position,
                        session.Day.ToString(),
                        session.Kind.ToString(),
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
public sealed record GeneratedSessionResponse(
    int Position,
    string Day,
    string Kind,
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

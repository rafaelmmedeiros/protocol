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

                var generatedAt = DateTimeOffset.UtcNow;
                var plan = WeekGenerator.Generate(
                    profile,
                    catalogue,
                    DateOnly.FromDateTime(generatedAt.UtcDateTime));

                // A new row every time. Regenerating is expected and never edits what is already
                // stored -- the week the user trained under has to stay readable (ADR-003).
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

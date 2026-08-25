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

        MapEraseIfEnabled(app, group);

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

                // Parsed rather than bound to the enum, so an unknown name becomes a code the
                // frontend can translate instead of a framework error it cannot (root standard 3).
                SplitTemplateId? split = null;
                if (!string.IsNullOrWhiteSpace(request.Split))
                {
                    if (!Enum.TryParse<SplitTemplateId>(request.Split, ignoreCase: true, out var parsed)
                        || !Enum.IsDefined(parsed))
                    {
                        return Results.BadRequest(new ApiError(TrainingErrorCodes.SplitNotAdmitted));
                    }

                    split = parsed;
                }

                var error = TrainingProfileRules.Validate(
                    goal, request.DaysPerWeek, request.SessionDurationSeconds, split);

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
                        Split = split,
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
                    // Absent means "no choice", which is a value rather than a field left alone:
                    // clearing it is how a user goes back to the frequency's default (ADR-030).
                    profile.Split = split;
                }

                await db.SaveChangesAsync(token);
                return Results.Ok(TrainingProfileResponse.From(profile));
            })
            .WithName("PutTrainingProfile");

        // The whole table rather than one frequency's row, because the frequency it depends on
        // is being edited on the screen that asks. Serving one row would make the answer stale
        // the moment the user changes the number -- and a first-time user has no saved frequency
        // at all, so there would be no row to serve (TD-023, ADR-030).
        group.MapGet("/splits", () => Results.Ok(
                Enumerable
                    .Range(TrainingProfileRules.MinDaysPerWeek,
                        TrainingProfileRules.MaxDaysPerWeek - TrainingProfileRules.MinDaysPerWeek + 1)
                    .Select(days => new SplitOptionsResponse(
                        days,
                        [.. SplitTemplate.Admitted(days).Select(id => id.ToString())],
                        SplitTemplate.Default(days).ToString()))
                    .ToList()))
            .WithName("GetSplitOptions");

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

        group.MapGet("/equipment/suggestions", async (
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var userId = UserIdOf(user);

                var performed = await db.PerformedWorkouts
                    .AsNoTracking()
                    .Include(w => w.Exercises)
                    .Where(w => w.UserId == userId)
                    .ToListAsync(token);

                var catalogue = await db.Exercises
                    .AsNoTracking()
                    .Include(e => e.Requirements)
                    .ToDictionaryAsync(exercise => exercise.Id, token);

                var declined = await db.DeclinedEquipmentSuggestions
                    .AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => row.Item)
                    .ToListAsync(token);

                return Results.Ok(DerivedEquipment.From(
                    PerformedVolume.Current(performed),
                    catalogue,
                    await OwnedItemsAsync(db, userId, token),
                    declined.ToHashSet()));
            })
            .WithName("GetEquipmentSuggestions");

        group.MapPost("/equipment/suggestions", async (
                EquipmentSuggestionRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                if (!Enum.TryParse<EquipmentItem>(request.Item, ignoreCase: true, out var item)
                    || !Enum.IsDefined(item))
                {
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.UnknownEquipmentItem));
                }

                var userId = UserIdOf(user);

                if (!request.Accepted)
                {
                    var already = await db.DeclinedEquipmentSuggestions
                        .AnyAsync(row => row.UserId == userId && row.Item == item, token);

                    if (!already)
                    {
                        db.DeclinedEquipmentSuggestions.Add(new DeclinedEquipmentSuggestion
                        {
                            Id = Guid.CreateVersion7(),
                            UserId = userId,
                            Item = item,
                        });

                        await db.SaveChangesAsync(token);
                    }

                    // Declining changes the equipment set not at all. It only stops the offer
                    // returning on every sync (ADR-020).
                    return Results.Ok(new EquipmentResponse(
                        [.. (await OwnedItemsAsync(db, userId, token)).Select(i => i.ToString()).Order()],
                        [.. Enum.GetValues<EquipmentItem>().Select(i => i.ToString())]));
                }

                // Seeded from the **effective** set, not from the rows. A user who never opened
                // the equipment screen has no rows and trains against the assumed gym (TD-004);
                // writing a single row for the accepted item would silently replace a whole gym
                // with one machine. Add-only means add to what is in force, not to what is stored.
                var effective = await OwnedItemsAsync(db, userId, token);
                var widened = effective.ToHashSet();

                if (!widened.Add(item))
                {
                    return Results.Ok(new EquipmentResponse(
                        [.. widened.Select(i => i.ToString()).Order()],
                        [.. Enum.GetValues<EquipmentItem>().Select(i => i.ToString())]));
                }

                var current = await db.UserEquipment.Where(row => row.UserId == userId).ToListAsync(token);
                db.UserEquipment.RemoveRange(current);
                db.UserEquipment.AddRange(widened.Select(owned => new UserEquipment
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    Item = owned,
                }));

                await db.SaveChangesAsync(token);

                return Results.Ok(new EquipmentResponse(
                    [.. widened.Select(i => i.ToString()).Order()],
                    [.. Enum.GetValues<EquipmentItem>().Select(i => i.ToString())]));
            })
            .WithName("AnswerEquipmentSuggestion");

        group.MapGet("/preferences", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var userId = UserIdOf(user);

                // Two queries rather than a join: projecting a join across DbSets into a record
                // does not translate, and this is three rows on a good day.
                var excludedIds = await db.ExerciseExclusions
                    .AsNoTracking()
                    .Where(row => row.UserId == userId)
                    .Select(row => row.ExerciseId)
                    .ToListAsync(token);

                var excluded = await ExcludedWithTitlesAsync(db, excludedIds, token);

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
                    await ExcludedWithTitlesAsync(db, excluded, token),
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
                    // Ordered because an unordered read is a whim of the database, and the
                    // generator's answer must not be one (ADR-005). The generator's own comparator
                    // is total as well; this is the cheaper half of the same guarantee.
                    .OrderBy(exercise => exercise.Id)
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

        group.MapGet("/weeks/{weekId:guid}/comparison", async (
                Guid weekId,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var userId = UserIdOf(user);

                var week = await db.GeneratedWeeks
                    .AsNoTracking()
                    .Include(w => w.Sessions).ThenInclude(s => s.Prescriptions).ThenInclude(p => p.Exercise)
                    .SingleOrDefaultAsync(w => w.Id == weekId && w.UserId == userId, token);

                if (week is null)
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.WeekNotFound));
                }

                // Every version, because the builder decides which reading counts -- a workout the
                // user deleted upstream must read as not performed rather than as history.
                var performed = await db.PerformedWorkouts
                    .AsNoTracking()
                    .Include(w => w.Exercises).ThenInclude(e => e.Sets)
                    .Where(w => w.UserId == userId)
                    .ToListAsync(token);

                return Results.Ok(WeekComparisonBuilder.Build(week, performed));
            })
            .WithName("GetWeekComparison");

        group.MapGet("/weeks/current/prescriptions/{id:guid}/candidates", async (
                Guid id,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var userId = UserIdOf(user);
                var week = await CurrentWeekAsync(db, userId, token);
                var prescription = week?.Sessions
                    .SelectMany(session => session.Prescriptions)
                    .SingleOrDefault(p => p.Id == id);

                if (week is null || prescription is null)
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.PrescriptionNotFound));
                }

                var candidates = await CandidatesForAsync(db, userId, prescription, token);

                return Results.Ok(candidates
                    .Select(exercise => new CandidateResponse(
                        exercise.Id,
                        exercise.Title,
                        exercise.ExternalTemplateId,
                        exercise.Equipment.ToString(),
                        exercise.OrderClass.ToString()))
                    .ToList());
            })
            .WithName("GetSubstitutionCandidates");

        group.MapPost("/weeks/current/prescriptions/{id:guid}/substitute", async (
                Guid id,
                SubstituteRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                var userId = UserIdOf(user);
                var week = await CurrentWeekAsync(db, userId, token);
                var prescription = week?.Sessions
                    .SelectMany(session => session.Prescriptions)
                    .SingleOrDefault(p => p.Id == id);

                if (week is null || prescription is null)
                {
                    return Results.NotFound(new ApiError(TrainingErrorCodes.PrescriptionNotFound));
                }

                var candidates = await CandidatesForAsync(db, userId, prescription, token);
                var replacement = candidates.SingleOrDefault(exercise => exercise.Id == request.ExerciseId);

                if (replacement is null)
                {
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.NotACandidate));
                }

                var catalogue = await CatalogueAsync(db, token);

                // A new week with one slot replaced, rather than an edit. The previous week stays
                // readable because someone may have trained it (ADR-003, ADR-012).
                var now = DateTimeOffset.UtcNow;
                var replaced = Substitute(
                    week,
                    prescription,
                    replacement,
                    new DateTimeOffset(now.Ticks - (now.Ticks % 10), now.Offset),
                    InferCut(week, catalogue));

                db.GeneratedWeeks.Add(replaced);
                await db.SaveChangesAsync(token);

                return Results.Ok(GeneratedWeekResponse.From(replaced, [.. catalogue.Values]));
            })
            .WithName("SubstitutePrescription");

        return app;
    }

    /// <summary>
    /// What a slot could be swapped for: the same movement, training the same thing, performable
    /// in this user's gym, and not something they have excluded.
    /// <para>
    /// The candidate set needs no column of its own — <c>movement_pattern</c> plus the primary
    /// muscle already identify it, which is why `TD-015`'s anticipated <c>movement_group</c> tag
    /// has not been needed (`ADR-012`).
    /// </para>
    /// </summary>
    private static async Task<List<Exercise>> CandidatesForAsync(
        AppDbContext db,
        string userId,
        GeneratedPrescription prescription,
        CancellationToken token)
    {
        var catalogue = await CatalogueAsync(db, token);
        if (!catalogue.TryGetValue(prescription.ExerciseId, out var current)) return [];

        var owned = await OwnedItemsAsync(db, userId, token);
        var preferences = await PreferencesOf(db, userId, token);
        var primary = current.Muscles.Single(muscle => muscle.Role == MuscleRole.Primary).MuscleGroup;

        return
        [
            .. catalogue.Values
                .Where(exercise => exercise.Id != current.Id)
                .Where(exercise => exercise.MovementPattern == current.MovementPattern)
                .Where(exercise => exercise.Muscles
                    .Single(muscle => muscle.Role == MuscleRole.Primary).MuscleGroup == primary)
                .Where(exercise => !preferences.ExcludedExerciseIds.Contains(exercise.Id))
                .Where(exercise => exercise.Requirements.All(r => owned.Contains(r.Item)))
                .OrderBy(exercise => exercise.OrderClass)
                .ThenBy(exercise => exercise.PreferenceRank)
                .ThenBy(exercise => exercise.Equipment),
        ];
    }

    /// <summary>
    /// Copies a week, replacing one prescription. The replacement's numbers come from **its own**
    /// <see cref="OrderClass"/> rather than from the slot it replaces, so swapping a barbell
    /// press for a dumbbell one changes the repetition range and the rest along with it
    /// (`TD-009`, `TD-011`, `ADR-012`). Proximity to failure no longer moves on a swap — it is
    /// two everywhere (`TD-018`).
    /// </summary>
    private static GeneratedWeek Substitute(
        GeneratedWeek week,
        GeneratedPrescription replacedSlot,
        Exercise replacement,
        DateTimeOffset generatedAt,
        CutLevel cut)
    {
        var prescription = TrainingPrescription.For(replacement.OrderClass);

        // The set count is the replaced slot's own, not the week's cut level re-derived. Since
        // TD-022 a session mixes three-set slots with two-set ones bought above the guaranteed
        // target, so re-deriving would silently promote a phase-2 slot to a full one and push the
        // muscle past the ceiling on a swap. Reps, reserve and rest still come from the
        // replacement's own OrderClass (ADR-012) -- what a slot *is* comes from the exercise, how
        // much of it there is was decided when the week was generated.
        var sets = replacedSlot.Sets; // TD-022
        var rest = cut == CutLevel.None
            ? prescription.RestSeconds                  // TD-011
            : TrainingPrescription.RestFloorSeconds;    // TD-011, never below

        return new GeneratedWeek
        {
            Id = Guid.CreateVersion7(),
            UserId = week.UserId,
            WeekStartDate = week.WeekStartDate,
            GeneratedAt = generatedAt,
            Goal = week.Goal,
            DaysPerWeek = week.DaysPerWeek,
            SessionDurationSeconds = week.SessionDurationSeconds,
            // The band comes from the week being copied, never from the constant: a substitution
            // produces a new row describing the same plan, and re-reading today's value would
            // silently re-judge it under rules it was not generated under (ADR-003, ADR-029).
            WeeklyTargetFractionalSets = week.WeeklyTargetFractionalSets,
            WeeklyCeilingFractionalSets = week.WeeklyCeilingFractionalSets,
            Sessions =
            [
                .. week.Sessions.OrderBy(session => session.Position).Select(session => new GeneratedSession
                {
                    Id = Guid.CreateVersion7(),
                    Position = session.Position,
                    Day = session.Day,
                    Kind = session.Kind,
                    Prescriptions =
                    [
                        .. session.Prescriptions.OrderBy(p => p.Position).Select(p => new GeneratedPrescription
                        {
                            Id = Guid.CreateVersion7(),
                            Position = p.Position,
                            ExerciseId = p.Id == replacedSlot.Id ? replacement.Id : p.ExerciseId,
                            Sets = p.Id == replacedSlot.Id ? sets : p.Sets,
                            MinReps = p.Id == replacedSlot.Id ? prescription.MinReps : p.MinReps,
                            MaxReps = p.Id == replacedSlot.Id ? prescription.MaxReps : p.MaxReps,
                            RepsInReserve = p.Id == replacedSlot.Id ? prescription.RepsInReserve : p.RepsInReserve,
                            RestSeconds = p.Id == replacedSlot.Id ? rest : p.RestSeconds,
                        }),
                    ],
                }),
            ],
        };
    }

    /// <summary>
    /// How far down `TD-013`'s ladder the stored week was generated, read back from what it
    /// contains. A substitution has to land on the same rung, or one slot would rest three
    /// minutes in a week where every other slot rests ninety seconds.
    /// </summary>
    internal static CutLevel InferCut(GeneratedWeek week, IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var slots = week.Sessions.SelectMany(session => session.Prescriptions).ToList();
        if (slots.Count == 0) return CutLevel.None;

        // Every slot, not any. TD-013 spreads a set cut evenly across the whole week and never
        // concentrates it, so a week where *some* slots carry fewer sets is not a cut week -- it
        // is a week where phase 2 bought slots above the guaranteed target, and those carry two
        // sets by TD-022. Reading `any` here would report a full ladder descent for a week that
        // cut nothing, and a substitution would then rewrite the replacement at floor rest.
        if (slots.All(slot => slot.Sets < TrainingPrescription.SetsPerSlot))
        {
            return CutLevel.RestToFloorAndFewerSets;
        }

        var restWasCut = slots.Any(slot =>
            catalogue.TryGetValue(slot.ExerciseId, out var exercise)
            && slot.RestSeconds < TrainingPrescription.For(exercise.OrderClass).RestSeconds);

        return restWasCut ? CutLevel.RestToFloor : CutLevel.None;
    }

    private static Task<GeneratedWeek?> CurrentWeekAsync(
        AppDbContext db,
        string userId,
        CancellationToken token) =>
        db.GeneratedWeeks
            .Include(week => week.Sessions).ThenInclude(session => session.Prescriptions)
            .Where(week => week.UserId == userId)
            .OrderByDescending(week => week.GeneratedAt)
            .FirstOrDefaultAsync(token);

    private static async Task<Dictionary<Guid, Exercise>> CatalogueAsync(
        AppDbContext db,
        CancellationToken token) =>
        await db.Exercises
            .Include(exercise => exercise.Muscles)
            .Include(exercise => exercise.Requirements)
            .AsNoTracking()
            .ToDictionaryAsync(exercise => exercise.Id, token);

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
            WeeklyTargetFractionalSets = TrainingPrescription.WeeklyTargetFractionalSets,   // TD-014
            WeeklyCeilingFractionalSets = TrainingPrescription.WeeklyCeilingFractionalSets, // TD-022
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
    /// <summary>
    /// Excluded exercises with the titles a screen needs to name them. Ids alone were enough for
    /// the API and are not enough for a person.
    /// </summary>
    private static async Task<List<ExcludedExerciseResponse>> ExcludedWithTitlesAsync(
        AppDbContext db,
        IReadOnlyCollection<Guid> ids,
        CancellationToken token)
    {
        if (ids.Count == 0) return [];

        var rows = await db.Exercises
            .AsNoTracking()
            .Where(exercise => ids.Contains(exercise.Id))
            .Select(exercise => new { exercise.Id, exercise.Title })
            .ToListAsync(token);

        return [.. rows.OrderBy(row => row.Title).Select(row => new ExcludedExerciseResponse(row.Id, row.Title))];
    }

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

    /// <summary>
    /// The erase endpoint, mapped only where the switch is set (ADR-025).
    /// <para>
    /// <b>Mapped, not guarded.</b> With the switch off the route does not exist and the router
    /// answers 404 — there is no check inside a handler that a later change could relax, and no
    /// documented endpoint that politely refuses. A feature justified by "we are still iterating"
    /// has to be absent where that is untrue, and absence is the only version of that which cannot
    /// be undone by accident.
    /// </para>
    /// </summary>
    private static void MapEraseIfEnabled(IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
        var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue<bool>(EraseUserData.EnabledKey))
        {
            return;
        }

        // The frontend has to know whether to draw the panel, and the switch is the API's. A second
        // flag on that tier could disagree with this one, and the disagreement would show up as a
        // button that 404s. One authority, probed.
        group.MapGet("/erase", () => Results.Ok(new EraseAvailability(true)))
            .WithName("EraseAvailability");

        group.MapPost("/erase", async (
                EraseRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                ILoggerFactory loggers,
                CancellationToken token) =>
            {
                // Deliberate, never a side effect (ADR-025). The confirmation is a required field
                // rather than a screen-only concern, so nothing reaches this by replaying a URL or
                // by a redirect that happened to be a POST.
                if (!request.Confirmed)
                {
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.EraseNotConfirmed));
                }

                var userId = UserIdOf(user);
                var erased = await EraseUserData.EraseAsync(db, userId, token);

                // With the counts, because afterwards "the data was erased" and "the import never
                // ran" look identical from every screen (root standard 12).
                loggers.CreateLogger(typeof(EraseUserData)).LogWarning(
                    "Erased everything for {UserId}: {Profiles} profile, {Equipment} equipment, "
                    + "{Exclusions} exclusions, {PreferredVariants} preferred variants, "
                    + "{Declined} declined suggestions, {Weeks} generated weeks, "
                    + "{Snapshots} snapshots, {Workouts} imported workouts, "
                    + "{Connections} Hevy connections. The catalogue and the key ring were not "
                    + "touched.",
                    userId,
                    erased.Profiles,
                    erased.Equipment,
                    erased.Exclusions,
                    erased.PreferredVariants,
                    erased.DeclinedSuggestions,
                    erased.GeneratedWeeks,
                    erased.Snapshots,
                    erased.PerformedWorkouts,
                    erased.HevyConnections);

                return Results.Ok(erased);
            })
            .WithName("EraseUserData");
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
public sealed record TrainingProfileRequest(
    string? Goal,
    int DaysPerWeek,
    int SessionDurationSeconds,
    /// <summary>A SplitTemplateId name, or null for the frequency's default (ADR-030).</summary>
    string? Split);

/// <summary>
/// A profile as the API returns it. Duration is seconds, always: minutes are a rendering
/// concern and are converted at the edge (root standard 4).
/// </summary>
public sealed record TrainingProfileResponse(
    string Goal,
    int DaysPerWeek,
    int SessionDurationSeconds,

    /// <summary>What the user chose, or null when they never did (ADR-030).</summary>
    string? Split,

    /// <summary>What that resolves to today — the choice, or the frequency's default.</summary>
    string ResolvedSplit,

    /// <summary>
    /// What this frequency admits (TD-023). Travels with the answer so the screen never
    /// hardcodes a list that would drift from the record.
    /// </summary>
    IReadOnlyList<string> AdmittedSplits)
{
    public static TrainingProfileResponse From(TrainingProfile profile) =>
        new(
            profile.Goal.ToString(),
            profile.DaysPerWeek,
            profile.SessionDurationSeconds,
            profile.Split?.ToString(),
            SplitTemplate.Resolve(profile.Split, profile.DaysPerWeek).ToString(),
            [.. SplitTemplate.Admitted(profile.DaysPerWeek).Select(id => id.ToString())]);
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

    /// <summary>
    /// What every muscle group the catalogue trains directly receives this cycle, direct and
    /// indirect kept apart (TD-006), against the target this week was generated under (ADR-029).
    /// </summary>
    IReadOnlyList<MuscleVolumeResponse> Volume,

    IReadOnlyList<MuscleCoverageResponse> Shortfalls,

    /// <summary>
    /// Muscle groups no catalogue exercise trains directly. A different failure from a shortfall
    /// and reported apart from one: training more does not close it, so it is not the user's to
    /// fix (TD-013).
    /// </summary>
    IReadOnlyList<string> Uncovered,

    IReadOnlyList<GeneratedSessionResponse> Sessions)
{
    /// <summary>
    /// Per-muscle fractional volume, recomputed from the prescriptions actually stored.
    /// <para>
    /// Not a column: it is derivable, and a derived column can disagree with its own source. It
    /// is recomputed rather than inherited so that a substitution which starves a muscle says so
    /// on the week that starved it, not on the one before (`ADR-012`, `TD-008`).
    /// </para>
    /// </summary>
    internal static IReadOnlyList<MuscleVolumeResponse> VolumeOf(
        GeneratedWeek week,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var volumes = PrescribedVolume.ByMuscle(
            week.Sessions
                .SelectMany(session => session.Prescriptions)
                .Select(prescription => (
                    Exercise: catalogue.GetValueOrDefault(prescription.ExerciseId),
                    prescription.Sets))
                .Where(slot => slot.Exercise is not null)
                .Select(slot => (slot.Exercise!, slot.Sets)));

        // Every muscle the catalogue trains directly, so a muscle at zero is reported as zero
        // rather than by being absent from the list.
        return
        [
            .. catalogue.Values
                .Select(exercise => exercise.Muscles.Single(m => m.Role == MuscleRole.Primary).MuscleGroup)
                .Distinct()
                .Order()
                .Select(muscle => new MuscleVolumeResponse(
                    muscle.ToString(),
                    volumes.GetValueOrDefault(muscle).Direct,
                    volumes.GetValueOrDefault(muscle).Indirect,
                    // The week's own target, never today's constant: a week generated under a
                    // superseded number must not be re-judged under the current one (ADR-003,
                    // ADR-029). The window is a cycle (TD-024).
                    week.WeeklyTargetFractionalSets)),
        ];
    }

    /// <summary>
    /// Per-muscle fractional volume, recomputed from the prescriptions actually stored.
    /// <para>
    /// Not a column: it is derivable, and a derived column can disagree with its own source. It
    /// is recomputed rather than inherited so that a substitution which starves a muscle says so
    /// on the week that starved it, not on the one before (`ADR-012`, `TD-008`).
    /// </para>
    /// </summary>
    internal static IReadOnlyList<MuscleCoverageResponse> ShortfallsOf(
        IReadOnlyList<MuscleVolumeResponse> volume) =>
        [
            .. volume
                .Where(entry => entry.Direct + entry.Indirect < TrainingPrescription.WeeklyFloorFractionalSets) // TD-008
                .Select(entry => new MuscleCoverageResponse(
                    entry.MuscleGroup,
                    entry.Direct + entry.Indirect,
                    entry.Target)),
        ];

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

        // Read back from what the week contains rather than stored, exactly as a substitution
        // reads it -- and it is what separates a ceiling slot from a cut one below (TD-022).
        var cut = TrainingEndpoints.InferCut(week, titles);
        var volume = VolumeOf(week, titles);

        return new GeneratedWeekResponse(
            week.Id,
            week.WeekStartDate,
            week.GeneratedAt,
            week.Goal.ToString(),
            week.DaysPerWeek,
            week.SessionDurationSeconds,
            week.Sessions.Sum(EstimatedSecondsFor),
            volume,
            ShortfallsOf(volume),
            [.. PrescribedVolume.UncoveredBy(catalogue).Select(muscle => muscle.ToString())],
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
                                    titles.GetValueOrDefault(prescription.ExerciseId),
                                    cut)),
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
    /// <summary>
    /// The slot's own identifier. Absent until `S2.6`, which is when a screen first needed to
    /// address one — `S2.4` predicted this would be a response change rather than a model one.
    /// </summary>
    Guid Id,
    int Position,
    Guid ExerciseId,
    string ExerciseTitle,
    string ExternalTemplateId,
    int Sets,
    int MinReps,
    int MaxReps,
    int RepsInReserve,
    int RestSeconds,

    /// <summary>
    /// What the slot exists to train, and what it loads on the way. Every entry is an enum name;
    /// the frontend owns the words (root standard 3, root standard 2). This is the answer to
    /// "why is this exercise here" -- the generator's own reason is that this muscle was furthest
    /// from its target and this exercise trains it (ADR-029).
    /// </summary>
    IReadOnlyList<ExerciseMuscleResponse> Muscles,

    /// <summary>
    /// The class that decided the repetition range and the rest interval (TD-009, TD-011). A
    /// substitution takes the replacement's class, not this one, which is why a swap changes both
    /// numbers (ADR-012).
    /// </summary>
    string OrderClass,
    string MovementPattern,
    string Equipment,

    /// <summary>
    /// Whether the slot was drawn for the guaranteed target or bought above it (TD-022). Not
    /// derivable from <c>sets</c> alone: a fully cut week carries two sets everywhere for the
    /// opposite reason.
    /// </summary>
    string SlotKind)
{
    public static GeneratedPrescriptionResponse From(
        GeneratedPrescription prescription,
        Exercise? exercise,
        CutLevel cut) =>
        new(
            prescription.Id,
            prescription.Position,
            prescription.ExerciseId,
            exercise?.Title ?? string.Empty,
            exercise?.ExternalTemplateId ?? string.Empty,
            prescription.Sets,
            prescription.MinReps,
            prescription.MaxReps,
            prescription.RepsInReserve,
            prescription.RestSeconds,
            [
                .. (exercise?.Muscles ?? [])
                    .OrderBy(muscle => muscle.Role)
                    .ThenBy(muscle => muscle.MuscleGroup)
                    .Select(muscle => new ExerciseMuscleResponse(
                        muscle.MuscleGroup.ToString(),
                        muscle.Role.ToString())),
            ],
            exercise?.OrderClass.ToString() ?? string.Empty,
            exercise?.MovementPattern.ToString() ?? string.Empty,
            exercise?.Equipment.ToString() ?? string.Empty,
            TrainingPrescription.KindOf(prescription.Sets, cut).ToString()); // TD-022
}

/// <summary>One muscle a slot loads, and how. Enum names only (root standard 3).</summary>
public sealed record ExerciseMuscleResponse(string MuscleGroup, string Role);

/// <summary>
/// What the user has, and everything they could say they have. The vocabulary travels with the
/// answer so the screen never hardcodes a list that would drift from the enum.
/// </summary>
public sealed record EquipmentResponse(IReadOnlyList<string> Items, IReadOnlyList<string> Vocabulary);

/// <summary>
/// What one frequency may be arranged into (TD-023). Enum names, never sentences: the frontend
/// owns every word (root standard 3).
/// </summary>
public sealed record SplitOptionsResponse(
    int DaysPerWeek,
    IReadOnlyList<string> Templates,
    string Default);

/// <summary>
/// An answer to one suggestion. <c>Accepted</c> adds the item; anything else records a refusal
/// and changes nothing (ADR-020) — inference never removes.
/// </summary>
public sealed record EquipmentSuggestionRequest(string? Item, bool Accepted);

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
    IReadOnlyList<ExcludedExerciseResponse> Excluded,
    IReadOnlyList<PreferredVariantResponse> PreferredVariants);

/// <summary>
/// An excluded exercise, with the title needed to show it back. Ids alone were enough for the
/// API and are not enough for a screen that has to name what the user refused — found by
/// building that screen, not by predicting it.
/// </summary>
public sealed record ExcludedExerciseResponse(Guid ExerciseId, string Title);

/// <summary>The exercise chosen for a movement pattern, whenever that pattern is needed.</summary>
public sealed record PreferredVariantResponse(string MovementPattern, Guid ExerciseId);

public sealed record PreferencesRequest(
    IReadOnlyList<Guid>? ExcludedExerciseIds,
    IReadOnlyList<PreferredVariantRequest>? PreferredVariants);

public sealed record PreferredVariantRequest(string MovementPattern, Guid ExerciseId);

/// <summary>
/// A muscle that finished the week below TD-008's floor, with the number. "Rear delts reach 2.0
/// of 6.0" is arithmetic and defensible; "your programme is inadequate" is a growth claim with
/// nothing behind it (TD-015, TD-016).
/// </summary>
public sealed record MuscleCoverageResponse(string MuscleGroup, decimal FractionalSets, decimal Target);

/// <summary>
/// One muscle group's share of a cycle. Direct and indirect are separate because they are
/// different prescriptions producing the same total (TD-006), and <c>target</c> is the week's own
/// rather than today's constant (ADR-029).
/// </summary>
public sealed record MuscleVolumeResponse(
    string MuscleGroup,
    decimal Direct,
    decimal Indirect,
    decimal Target);

/// <summary>An exercise a slot could be swapped for.</summary>
public sealed record CandidateResponse(
    Guid ExerciseId,
    string Title,
    string ExternalTemplateId,
    string Equipment,
    string OrderClass);

public sealed record SubstituteRequest(Guid ExerciseId);

/// <summary>
/// Confirmation that the erase is meant. A field rather than a screen concern, so the deliberate
/// half of ADR-025 lives where it cannot be routed around.
/// </summary>
public sealed record EraseRequest(bool Confirmed);

/// <summary>
/// That the erase endpoint exists. Only ever reachable when it does — the alternative reading, a
/// 404, is what the frontend acts on.
/// </summary>
public sealed record EraseAvailability(bool Available);

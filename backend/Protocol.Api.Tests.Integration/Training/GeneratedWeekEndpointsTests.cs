using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Generating and reading a week, over HTTP against a real Postgres.
/// </summary>
public class GeneratedWeekEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private async Task<HttpClient> SignedInClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        return client;
    }

    private static async Task SetProfileAsync(HttpClient client, int days, int seconds = 3_600)
    {
        var response = await client.PutAsJsonAsync(
            "/training/profile",
            new { goal = "Hypertrophy", daysPerWeek = days, sessionDurationSeconds = seconds });
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> UserIdAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<Dictionary<string, object>>("/auth/me");
        var email = me!["email"].ToString();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return (await db.Users.SingleAsync(user => user.Email == email)).Id;
    }

    /// <summary>
    /// Every imported workout in the database, not just this user's. A declaration must not write
    /// one anywhere, and counting the whole table is what makes that assertion mean it.
    /// </summary>
    private async Task<int> PerformedWorkoutCountAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.PerformedWorkouts.CountAsync();
    }

    private static async Task<GeneratedWeekResponse> GenerateAsync(HttpClient client)
    {
        var response = await client.PostAsync("/training/weeks", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;
    }

    [Fact]
    public async Task Weeks_are_unreachable_without_a_session()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/training/weeks", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/training/weeks/current")).StatusCode);
    }

    [Fact]
    public async Task Generating_without_a_profile_reports_the_missing_profile()
    {
        var client = await SignedInClientAsync();

        var response = await client.PostAsync("/training/weeks", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.ProfileNotFound, error!.Code);
    }

    [Fact]
    public async Task Reading_before_generating_reports_no_week_rather_than_an_error()
    {
        // The frontend turns this into the empty state: a user with no week has done nothing
        // wrong.
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var response = await client.GetAsync("/training/weeks/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.WeekNotFound, error!.Code);
    }

    [Fact]
    public async Task A_generated_plan_matches_the_profile_and_carries_no_dates()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);

        // ADR-027: an ordered queue. The date and the weekday are gone from what is generated
        // and kept only on rows that predate the record.
        Assert.Null(week.WeekStartDate);
        Assert.All(week.Sessions, session => Assert.Null(session.Day));

        Assert.Equal(4, week.Sessions.Count);
        Assert.Equal("Hypertrophy", week.Goal);
        Assert.Equal([1, 2, 3, 4], week.Sessions.Select(session => session.Position));
        Assert.All(week.Sessions, session => Assert.NotEmpty(session.Prescriptions));
    }

    [Fact]
    public async Task Every_prescription_carries_its_numbers_and_its_exercise()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);
        var prescriptions = week.Sessions.SelectMany(session => session.Prescriptions).ToList();

        Assert.All(prescriptions, prescription =>
        {
            Assert.True(prescription.Sets > 0);
            Assert.True(prescription.MinReps > 0 && prescription.MaxReps >= prescription.MinReps);
            Assert.Equal(2, prescription.RepsInReserve);                  // TD-018, two everywhere
            Assert.True(prescription.RestSeconds >= 90);                  // TD-011, the floor
            Assert.False(string.IsNullOrWhiteSpace(prescription.ExerciseTitle));
            Assert.False(string.IsNullOrWhiteSpace(prescription.ExternalTemplateId));
        });
    }

    [Fact]
    public async Task Every_prescription_says_what_it_trains_and_which_class_decided_its_numbers()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);
        var prescriptions = week.Sessions.SelectMany(session => session.Prescriptions).ToList();

        Assert.NotEmpty(prescriptions);
        Assert.All(prescriptions, prescription =>
        {
            // Exactly one primary is the catalogue's own invariant (TD-005); the screen's
            // "why is this here" answer is that muscle.
            Assert.Single(prescription.Muscles, muscle => muscle.Role == nameof(MuscleRole.Primary));

            Assert.All(prescription.Muscles, muscle =>
            {
                Assert.True(Enum.TryParse<MuscleGroup>(muscle.MuscleGroup, out _), muscle.MuscleGroup);
                Assert.True(Enum.TryParse<MuscleRole>(muscle.Role, out _), muscle.Role);
            });

            // Enum names, never sentences (root standard 3) -- parsing is the assertion.
            Assert.True(Enum.TryParse<OrderClass>(prescription.OrderClass, out _), prescription.OrderClass);
            Assert.True(Enum.TryParse<MovementPattern>(prescription.MovementPattern, out _), prescription.MovementPattern);
            Assert.True(Enum.TryParse<Equipment>(prescription.Equipment, out _), prescription.Equipment);
            Assert.True(Enum.TryParse<SlotKind>(prescription.SlotKind, out _), prescription.SlotKind);
        });
    }

    [Fact]
    public async Task A_week_with_minutes_to_spare_marks_its_ceiling_slots()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 5, 3_600);

        var week = await GenerateAsync(client);
        var prescriptions = week.Sessions.SelectMany(session => session.Prescriptions).ToList();

        // Both kinds are present and told apart by the field rather than by inference: a full
        // slot carries three sets, a ceiling slot two, and in a *cut* week two would mean the
        // opposite thing (TD-022). The test below is the one that pins that distinction.
        Assert.Contains(prescriptions, p => p.SlotKind == nameof(SlotKind.Ceiling));
        Assert.Contains(prescriptions, p => p.SlotKind == nameof(SlotKind.Full));

        Assert.All(
            prescriptions.Where(p => p.SlotKind == nameof(SlotKind.Ceiling)),
            p => Assert.Equal(TrainingPrescription.CeilingSetsPerSlot, p.Sets));
    }

    [Fact]
    public async Task A_cut_week_has_no_ceiling_slots_however_few_sets_it_carries()
    {
        var client = await SignedInClientAsync();

        // Twenty-five minutes is TD-012's minimum and forces the ladder to its last rung, so
        // every slot carries two sets for TD-013's reason. None of them was bought.
        await SetProfileAsync(client, 2, 1_500);

        var week = await GenerateAsync(client);
        var prescriptions = week.Sessions.SelectMany(session => session.Prescriptions).ToList();

        Assert.NotEmpty(prescriptions);
        Assert.All(prescriptions, p => Assert.Equal(nameof(SlotKind.Full), p.SlotKind));
        Assert.All(prescriptions, p => Assert.Equal(TrainingPrescription.ReducedSetsPerSlot, p.Sets));
    }

    [Fact]
    public async Task Volume_is_reported_per_muscle_against_the_weeks_own_target()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);

        Assert.NotEmpty(week.Volume);
        Assert.All(week.Volume, entry =>
        {
            Assert.True(Enum.TryParse<MuscleGroup>(entry.MuscleGroup, out _), entry.MuscleGroup);
            Assert.Equal(TrainingPrescription.WeeklyTargetFractionalSets, entry.Target);
            Assert.True(entry.Direct >= 0 && entry.Indirect >= 0);
        });

        // A shortfall is a view onto the same measurement, never a second one.
        Assert.All(week.Shortfalls, shortfall =>
        {
            var entry = week.Volume.Single(v => v.MuscleGroup == shortfall.MuscleGroup);
            Assert.Equal(entry.Direct + entry.Indirect, shortfall.FractionalSets);
            Assert.True(shortfall.FractionalSets < TrainingPrescription.WeeklyFloorFractionalSets);
        });
    }

    [Fact]
    public async Task A_stored_week_keeps_its_own_target_when_the_constant_moves()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);
        var generated = await GenerateAsync(client);

        // The constant cannot move inside a test, so the stored value is moved instead -- which
        // is the same thing from the response's point of view and is the only way to prove the
        // comparison reads the week rather than TrainingPrescription. Written directly to the
        // context because no endpoint exposes this column, the standing exception backend
        // CLAUDE.md describes for a claim that is about storage.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.GeneratedWeeks.SingleAsync(week => week.Id == generated.Id);
            db.Entry(stored).Property(week => week.WeeklyTargetFractionalSets).CurrentValue = 8.0m;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/training/weeks/current");
        var reread = (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        Assert.NotEmpty(reread.Volume);
        Assert.All(reread.Volume, entry => Assert.Equal(8.0m, entry.Target));
        Assert.All(reread.Shortfalls, shortfall => Assert.Equal(8.0m, shortfall.Target));
    }

    [Fact]
    public async Task A_muscle_no_exercise_trains_directly_is_uncovered_rather_than_short()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);

        // Guards the assertions below against passing vacuously. Today this is exactly one
        // muscle, Adductors -- the only group no row of the 63-exercise catalogue trains
        // directly. **If this line ever fails, the catalogue grew an adductor exercise and the
        // right response is to re-decide this assertion, not to delete it**: an empty list would
        // make every Assert.All below true of nothing.
        Assert.NotEmpty(week.Uncovered);

        // The two lists are disjoint by construction: uncovered is about the catalogue, a
        // shortfall is about this week's time budget, and only the second is the user's to fix
        // (TD-013).
        Assert.All(week.Uncovered, muscle =>
        {
            Assert.True(Enum.TryParse<MuscleGroup>(muscle, out _), muscle);
            Assert.DoesNotContain(week.Volume, entry => entry.MuscleGroup == muscle);
            Assert.DoesNotContain(week.Shortfalls, entry => entry.MuscleGroup == muscle);
        });
    }

    [Fact]
    public async Task A_week_stored_before_the_queue_still_reads_with_its_dates()
    {
        // ADR-027 stopped generating dates and did not delete the ones already stored. A row
        // written under ADR-008 has to keep meaning what it meant -- root standard 7 for the
        // history, ADR-003 for the week specifically -- so it is inserted here exactly as the
        // old generator would have written it and read back through the API.
        //
        // Written to the context because no endpoint can produce this shape any more, which is
        // the standing exception backend CLAUDE.md describes for a claim about storage.
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 2);
        var userId = await UserIdAsync(client);

        var exercise = ExerciseCatalogue.All.First();
        var legacy = new GeneratedWeek
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            WeekStartDate = new DateOnly(2026, 8, 17),
            GeneratedAt = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero),
            Goal = TrainingGoal.Hypertrophy,
            DaysPerWeek = 2,
            SessionDurationSeconds = 3_600,
            WeeklyTargetFractionalSets = 6.0m,
            WeeklyCeilingFractionalSets = 6.0m,
            Sessions =
            [
                new GeneratedSession
                {
                    Id = Guid.CreateVersion7(),
                    Position = 1,
                    Day = DayOfWeek.Monday,
                    Kind = SessionKind.FullBody,
                    Prescriptions =
                    [
                        new GeneratedPrescription
                        {
                            Id = Guid.CreateVersion7(),
                            Position = 1,
                            ExerciseId = exercise.Id,
                            Sets = 3,
                            MinReps = 6,
                            MaxReps = 10,
                            RepsInReserve = 2,
                            RestSeconds = 180,
                        },
                    ],
                },
            ],
        };

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.GeneratedWeeks.Add(legacy);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/training/weeks/current");
        response.EnsureSuccessStatusCode();
        var week = (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        Assert.Equal(legacy.Id, week.Id);
        Assert.Equal(new DateOnly(2026, 8, 17), week.WeekStartDate);
        Assert.Equal("Monday", week.Sessions[0].Day);

        // And everything M5 added is computed for it too, against the target *it* was stored
        // with rather than today's constant.
        Assert.NotEmpty(week.Volume);
        Assert.All(week.Volume, entry => Assert.Equal(6.0m, entry.Target));
    }

    [Fact]
    public async Task A_fresh_plan_points_at_its_first_session()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);

        Assert.Equal(1, week.NextSessionPosition);
        Assert.All(week.Sessions, session => Assert.Equal(nameof(SessionOutcome.Pending), session.Outcome));
    }

    [Fact]
    public async Task Marking_a_session_advances_the_queue_and_writes_no_history()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);
        var week = await GenerateAsync(client);

        var before = await PerformedWorkoutCountAsync();

        var response = await client.PostAsync(
            $"/training/weeks/current/sessions/{week.Sessions[0].Id}/done", null);
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        Assert.Equal(nameof(SessionOutcome.Marked), updated.Sessions[0].Outcome);
        Assert.Equal(2, updated.NextSessionPosition);

        // Root standard 7: a declaration is about the plan and never about imported training.
        Assert.Equal(before, await PerformedWorkoutCountAsync());
    }

    [Fact]
    public async Task Skipping_a_session_advances_the_queue_and_is_not_a_completion()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);
        var week = await GenerateAsync(client);

        var before = await PerformedWorkoutCountAsync();

        var response = await client.PostAsync(
            $"/training/weeks/current/sessions/{week.Sessions[0].Id}/skip", null);
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        // The distinction ADR-032 exists for: the queue moves either way, and only one of the two
        // says the training happened.
        Assert.Equal(nameof(SessionOutcome.Skipped), updated.Sessions[0].Outcome);
        Assert.NotEqual(nameof(SessionOutcome.Marked), updated.Sessions[0].Outcome);
        Assert.Equal(2, updated.NextSessionPosition);
        Assert.Equal(before, await PerformedWorkoutCountAsync());
    }

    [Fact]
    public async Task A_declaration_can_be_corrected()
    {
        // Skipping and then marking is a correction rather than an error: the later statement is
        // the user's, and refusing it would leave them with a plan they cannot fix.
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);
        var week = await GenerateAsync(client);
        var sessionId = week.Sessions[0].Id;

        await client.PostAsync($"/training/weeks/current/sessions/{sessionId}/skip", null);
        var response = await client.PostAsync($"/training/weeks/current/sessions/{sessionId}/done", null);
        var updated = (await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        Assert.Equal(nameof(SessionOutcome.Marked), updated.Sessions[0].Outcome);
    }

    [Fact]
    public async Task A_plan_whose_sessions_have_all_left_the_queue_points_at_nothing()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 2);
        var week = await GenerateAsync(client);

        foreach (var session in week.Sessions)
        {
            await client.PostAsync($"/training/weeks/current/sessions/{session.Id}/skip", null);
        }

        var read = await client.GetFromJsonAsync<GeneratedWeekResponse>("/training/weeks/current");

        Assert.Null(read!.NextSessionPosition);
    }

    [Fact]
    public async Task Declaring_a_session_that_is_not_in_the_current_plan_is_refused_with_its_code()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);
        await GenerateAsync(client);

        var response = await client.PostAsync(
            $"/training/weeks/current/sessions/{Guid.CreateVersion7()}/done", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.SessionNotFound, error!.Code);
    }

    [Fact]
    public async Task The_current_week_is_the_one_most_recently_generated()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 3);

        var first = await GenerateAsync(client);
        await SetProfileAsync(client, 5);
        var second = await GenerateAsync(client);

        var current = await client.GetFromJsonAsync<GeneratedWeekResponse>("/training/weeks/current");

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(second.Id, current!.Id);
        Assert.Equal(5, current.DaysPerWeek);
    }

    [Fact]
    public async Task Editing_the_profile_does_not_reach_back_into_a_stored_week()
    {
        // The acceptance criterion, and the whole point of ADR-003's snapshot: a week whose
        // frequency changed when the user edited their profile would be a week nobody trained.
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 3, 3_600);

        var generated = await GenerateAsync(client);

        await SetProfileAsync(client, 6, 5_400);

        var current = await client.GetFromJsonAsync<GeneratedWeekResponse>("/training/weeks/current");

        Assert.Equal(generated.Id, current!.Id);
        Assert.Equal(3, current.DaysPerWeek);
        Assert.Equal(3_600, current.SessionDurationSeconds);
        Assert.Equal(3, current.Sessions.Count);
    }

    [Fact]
    public async Task A_user_never_reads_another_users_week()
    {
        var first = await SignedInClientAsync();
        await SetProfileAsync(first, 4);
        await GenerateAsync(first);

        var second = await SignedInClientAsync();
        await SetProfileAsync(second, 4);

        var response = await second.GetAsync("/training/weeks/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generating_twice_without_changing_anything_writes_nothing_the_second_time()
    {
        // The generator is deterministic (ADR-005), so an unchanged profile can only reproduce
        // what is stored. An identical row is the same answer written twice and explains
        // nothing, which is the whole justification ADR-003 gave for storing weeks (ADR-009).
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4, 3_600);

        var first = await GenerateAsync(client);
        var second = await GenerateAsync(client);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.GeneratedAt, second.GeneratedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.GeneratedWeeks.AsNoTracking().CountAsync(week => week.Id == first.Id);

        Assert.Equal(1, stored);
    }

    [Fact]
    public async Task Generating_twice_leaves_two_weeks_and_the_first_is_unchanged()
    {
        // Reads the context rather than the API, deliberately and for the same reason
        // ExerciseCatalogueTests does: the claim is about storage. ADR-003 says a regeneration
        // writes a new row and never edits one, and no endpoint exposes a week that is no longer
        // current -- so the only way to test the record is to look at the record.
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 3, 3_600);

        var first = await GenerateAsync(client);
        await SetProfileAsync(client, 6, 5_400);
        var second = await GenerateAsync(client);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.GeneratedWeeks
            .AsNoTracking()
            .Include(week => week.Sessions)
            .Where(week => week.Id == first.Id || week.Id == second.Id)
            .ToListAsync();

        Assert.Equal(2, stored.Count);

        var original = stored.Single(week => week.Id == first.Id);
        Assert.Equal(3, original.DaysPerWeek);
        Assert.Equal(3_600, original.SessionDurationSeconds);
        Assert.Equal(3, original.Sessions.Count);
    }

    [Fact]
    public async Task Generating_repeatedly_writes_one_week()
    {
        // ADR-009 refuses to write a week identical to the current one, and that guard only holds
        // if generation is deterministic (ADR-005). It was not: in production the same profile
        // alternated between two plans, so every regeneration differed from the one immediately
        // before it and every click wrote a row -- five weeks in fifteen seconds.
        //
        // This runs at the level the failure lived at: the catalogue comes from a real query, so
        // an unordered read would show up here where an in-memory list cannot.
        var client = await SignedInClientAsync();

        await client.PutAsJsonAsync("/training/equipment", new
        {
            items = new[]
            {
                "AdjustableBench", "Barbell", "Bench", "Bodyweight", "CableStation",
                "Dumbbells", "LatPulldownStation", "PullUpBar", "SquatRack", "WeightPlates",
            },
        });

        await client.PutAsJsonAsync("/training/profile", new
        {
            goal = "Hypertrophy",
            daysPerWeek = 5,
            sessionDurationSeconds = 3_600,
        });

        var bodies = new List<string>();

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var response = await client.PostAsync("/training/weeks", null);
            bodies.Add(await response.Content.ReadAsStringAsync());
        }

        // Same answer every time...
        Assert.Single(bodies.Distinct());

        var userId = (await client.GetFromJsonAsync<CurrentUser>("/auth/me"))!.Id;

        using var scope = factory.Services.CreateScope();
        // Counting rows because the number written is the whole point, and no endpoint reports it.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ...and one row for it.
        Assert.Equal(1, await db.GeneratedWeeks.CountAsync(week => week.UserId == userId));
    }
}

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
    public async Task A_generated_week_matches_the_profile_and_starts_on_Monday()
    {
        var client = await SignedInClientAsync();
        await SetProfileAsync(client, 4);

        var week = await GenerateAsync(client);

        Assert.Equal(DayOfWeek.Monday, week.WeekStartDate.DayOfWeek); // root standard 6
        Assert.Equal(4, week.Sessions.Count);
        Assert.Equal("Hypertrophy", week.Goal);
        Assert.Equal("Monday", week.Sessions[0].Day);
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

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Swapping one exercise. A week is immutable, so the swap writes a new one — and only the slot
/// that was asked about changes (`ADR-012`, `ADR-003`).
/// </summary>
public class SubstitutionTests(ApiFactory factory) : IClassFixture<ApiFactory>
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

    private static async Task<GeneratedWeekResponse> WeekForAsync(HttpClient client, int days = 4, int seconds = 5_400)
    {
        var profile = await client.PutAsJsonAsync(
            "/training/profile",
            new { goal = "Hypertrophy", daysPerWeek = days, sessionDurationSeconds = seconds });
        profile.EnsureSuccessStatusCode();

        var generated = await client.PostAsync("/training/weeks", null);
        generated.EnsureSuccessStatusCode();
        return (await generated.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;
    }

    private static IEnumerable<GeneratedPrescriptionResponse> SlotsOf(GeneratedWeekResponse week) =>
        week.Sessions.SelectMany(session => session.Prescriptions);

    /// <summary>
    /// Slot identifiers are not in the response — the API addresses a slot by id and the screen
    /// never needs one — so these come from the context.
    /// </summary>
    private async Task<List<(Guid Id, Guid ExerciseId)>> StoredSlotsAsync(Guid weekId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ordered explicitly. Without it Postgres is free to return rows in any order, and a
        // test that picks "the first slot" then means something different on two machines —
        // which is exactly how the previous version passed on the host and failed in Docker.
        return await db.GeneratedWeeks
            .Where(week => week.Id == weekId)
            .SelectMany(week => week.Sessions)
            .OrderBy(session => session.Position)
            .SelectMany(session => session.Prescriptions.OrderBy(prescription => prescription.Position))
            .Select(prescription => new ValueTuple<Guid, Guid>(prescription.Id, prescription.ExerciseId))
            .ToListAsync();
    }

    [Fact]
    public async Task Substituting_is_unreachable_without_a_session()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{Guid.NewGuid()}/substitute",
            new { exerciseId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_slot_that_is_not_in_the_current_week_is_refused()
    {
        var client = await SignedInClientAsync();
        await WeekForAsync(client);

        var response = await client.GetAsync(
            $"/training/weeks/current/prescriptions/{Guid.NewGuid()}/candidates");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.PrescriptionNotFound, error!.Code);
    }

    [Fact]
    public async Task Candidates_train_the_same_thing_and_exclude_the_slot_itself()
    {
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        var found = false;
        foreach (var (id, exerciseId) in slots)
        {
            var candidates = await client.GetFromJsonAsync<List<CandidateResponse>>(
                $"/training/weeks/current/prescriptions/{id}/candidates");

            Assert.NotNull(candidates);
            Assert.DoesNotContain(candidates!, candidate => candidate.ExerciseId == exerciseId);
            if (candidates!.Count > 0) found = true;
        }

        Assert.True(found, "at least one slot in a full gym should have an alternative");
    }

    [Fact]
    public async Task Substituting_writes_a_new_week_and_leaves_the_previous_one_untouched()
    {
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        var (slotId, replacement) = await FirstSwappableAsync(client, slots);

        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{slotId}/substitute",
            new { exerciseId = replacement });
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        Assert.NotEqual(week.Id, updated!.Id);

        // The previous week is still exactly what it was — someone may have trained it.
        var original = await StoredSlotsAsync(week.Id);
        Assert.Equal(slots, original);
    }

    [Fact]
    public async Task Only_the_named_slot_changes()
    {
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        var (slotId, replacement) = await FirstSwappableAsync(client, slots);
        var replacedExercise = slots.Single(slot => slot.Id == slotId).ExerciseId;

        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{slotId}/substitute",
            new { exerciseId = replacement });
        var updated = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var before = SlotsOf(week).Select(slot => slot.ExerciseId).ToList();
        var after = SlotsOf(updated!).Select(slot => slot.ExerciseId).ToList();

        Assert.Equal(before.Count, after.Count);
        var differences = before.Zip(after).Where(pair => pair.First != pair.Second).ToList();
        var difference = Assert.Single(differences);
        Assert.Equal(replacedExercise, difference.First);
        Assert.Equal(replacement, difference.Second);
    }

    [Fact]
    public async Task The_prescription_follows_the_replacement_rather_than_the_slot_it_replaces()
    {
        // ADR-012: a swap across an order_class boundary changes reps, RIR and rest with the
        // exercise. It looks like a bug and is the correct behaviour.
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        var (slotId, replacement) = await FirstSwappableAsync(client, slots, differentOrderClass: true);

        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{slotId}/substitute",
            new { exerciseId = replacement });
        var updated = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var slot = SlotsOf(updated!).Single(s => s.ExerciseId == replacement);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exercise = await db.Exercises.SingleAsync(e => e.Id == replacement);
        var expected = TrainingPrescription.For(exercise.OrderClass);

        Assert.Equal(expected.MinReps, slot.MinReps);
        Assert.Equal(expected.MaxReps, slot.MaxReps);
        Assert.Equal(expected.RepsInReserve, slot.RepsInReserve);
    }

    [Fact]
    public async Task An_exercise_that_trains_something_else_is_refused()
    {
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var calfRaise = await db.Exercises
            .SingleAsync(exercise => exercise.ExternalTemplateId == "6DA40660");

        // Not merely "a slot that is not this exercise" — the barbell calf raise is a different
        // exercise and a *legitimate* candidate for it, so that test passed only when the
        // unordered query happened to return something else first. The slot has to train a
        // different movement entirely.
        var patterns = await db.Exercises
            .AsNoTracking()
            .ToDictionaryAsync(exercise => exercise.Id, exercise => exercise.MovementPattern);

        var pressSlot = slots.First(slot => patterns[slot.ExerciseId] != calfRaise.MovementPattern);

        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{pressSlot.Id}/substitute",
            new { exerciseId = calfRaise.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.NotACandidate, error!.Code);
    }

    [Fact]
    public async Task A_candidate_the_gym_cannot_perform_is_never_offered()
    {
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync(
            "/training/equipment",
            new { items = new[] { "Bodyweight", "Barbell", "WeightPlates", "SquatRack", "Bench" } });

        var week = await WeekForAsync(client, days: 3);
        var slots = await StoredSlotsAsync(week.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalogue = await db.Exercises
            .Include(exercise => exercise.Requirements)
            .AsNoTracking()
            .ToDictionaryAsync(exercise => exercise.Id);

        var owned = new HashSet<EquipmentItem>
        {
            EquipmentItem.Bodyweight, EquipmentItem.Barbell,
            EquipmentItem.WeightPlates, EquipmentItem.SquatRack, EquipmentItem.Bench,
        };

        foreach (var (id, _) in slots)
        {
            var candidates = await client.GetFromJsonAsync<List<CandidateResponse>>(
                $"/training/weeks/current/prescriptions/{id}/candidates");

            Assert.All(candidates!, candidate => Assert.All(
                catalogue[candidate.ExerciseId].Requirements,
                requirement => Assert.Contains(requirement.Item, owned)));
        }
    }

    [Fact]
    public async Task The_shortfall_is_recomputed_on_the_new_week_rather_than_inherited()
    {
        var client = await SignedInClientAsync();
        var week = await WeekForAsync(client);
        var slots = await StoredSlotsAsync(week.Id);

        var (slotId, replacement) = await FirstSwappableAsync(client, slots);

        var response = await client.PostAsJsonAsync(
            $"/training/weeks/current/prescriptions/{slotId}/substitute",
            new { exerciseId = replacement });
        var updated = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        // Whatever it says, it says with a number and about the week that exists now (TD-016).
        Assert.All(updated!.Shortfalls, shortfall =>
        {
            Assert.True(shortfall.FractionalSets < shortfall.Target);
            Assert.False(string.IsNullOrWhiteSpace(shortfall.MuscleGroup));
        });
    }

    /// <summary>Finds a slot with at least one alternative, optionally in another order class.</summary>
    private async Task<(Guid SlotId, Guid Replacement)> FirstSwappableAsync(
        HttpClient client,
        List<(Guid Id, Guid ExerciseId)> slots,
        bool differentOrderClass = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var classes = await db.Exercises.AsNoTracking()
            .ToDictionaryAsync(exercise => exercise.Id, exercise => exercise.OrderClass);

        foreach (var (id, exerciseId) in slots)
        {
            var candidates = await client.GetFromJsonAsync<List<CandidateResponse>>(
                $"/training/weeks/current/prescriptions/{id}/candidates");

            var match = candidates!.FirstOrDefault(candidate =>
                !differentOrderClass || classes[candidate.ExerciseId] != classes[exerciseId]);

            if (match is not null) return (id, match.ExerciseId);
        }

        throw new InvalidOperationException(
            differentOrderClass
                ? "no slot had an alternative in another order class"
                : "no slot had an alternative");
    }
}

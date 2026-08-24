namespace Protocol.Api.Training;

/// <summary>
/// Equipment the training history reveals, offered as suggestions rather than applied (ADR-020).
/// <para>
/// The inference is sound and not certain: a logged exercise could be a one-off in a hotel gym, a
/// friend's garage, or a machine that has since been removed. What the equipment set contains
/// changes what the generator may draw (TD-016), so a wrong addition changes next week's
/// prescription — and the user is the authority on their own gym.
/// </para>
/// </summary>
public static class DerivedEquipment
{
    /// <summary>
    /// How many catalogue gaps to name.
    /// <para>
    /// A real account produced **3,798 logged exercises outside the catalogue** on its first
    /// import. An unbounded list is not a report, it is a wall — so the most-trained few are named
    /// and the rest is a count. Ordered by how often the movement appears rather than by recency,
    /// because what to add to the catalogue is decided by what someone actually trains.
    /// </para>
    /// </summary>
    private const int GapsNamed = 20;

    /// <summary>
    /// What the history implies that the user has not already got, minus anything they declined.
    /// </summary>
    /// <param name="performed">Imported training — current readings only.</param>
    /// <param name="catalogue">Our exercises, by identifier.</param>
    /// <param name="owned">The user's effective equipment set today.</param>
    /// <param name="declined">Items already offered and refused. Not offered again (ADR-020).</param>
    public static EquipmentSuggestions From(
        IReadOnlyList<PerformedWorkout> performed,
        IReadOnlyDictionary<Guid, Exercise> catalogue,
        IReadOnlySet<EquipmentItem> owned,
        IReadOnlySet<EquipmentItem> declined)
    {
        var suggestions = new Dictionary<EquipmentItem, SuggestedEquipment>();
        var gaps = new Dictionary<string, CatalogueGap>();
        var gapCounts = new Dictionary<string, int>();

        foreach (var (workout, exercise) in performed
            .SelectMany(workout => workout.Exercises.Select(exercise => (workout, exercise))))
        {
            if (exercise.ExerciseId is not { } id || !catalogue.TryGetValue(id, out var ours))
            {
                // A movement we do not model implies no equipment, because there is no
                // requirement set to read. That is a gap in the catalogue rather than in the gym,
                // and it is surfaced separately rather than silently ignored (TD-004).
                var gap = gaps.GetValueOrDefault(exercise.ExternalTemplateId);
                var seen = gapCounts.GetValueOrDefault(exercise.ExternalTemplateId) + 1;
                gapCounts[exercise.ExternalTemplateId] = seen;

                gaps[exercise.ExternalTemplateId] = new CatalogueGap(
                    exercise.ExternalTemplateId,
                    exercise.ExternalTitle ?? gap?.Title,
                    Later(gap?.LastTrainedAt, workout.StartedAt),
                    seen);

                continue;
            }

            foreach (var requirement in ours.Requirements)
            {
                if (owned.Contains(requirement.Item) || declined.Contains(requirement.Item))
                {
                    continue;
                }

                var existing = suggestions.GetValueOrDefault(requirement.Item);

                // Each suggestion cites its evidence: which logged exercise implied it, and when.
                // A suggestion the user cannot audit is an assertion.
                suggestions[requirement.Item] = new SuggestedEquipment(
                    requirement.Item.ToString(),
                    existing?.ImpliedByTitle ?? ours.Title,
                    existing?.ImpliedByExternalTemplateId ?? ours.ExternalTemplateId,
                    Later(existing?.LastTrainedAt, workout.StartedAt));
            }
        }

        return new EquipmentSuggestions(
            [.. suggestions.Values.OrderByDescending(item => item.LastTrainedAt)],
            [.. gaps.Values.OrderByDescending(gap => gap.TimesTrained).Take(GapsNamed)],
            gaps.Count);
    }

    private static DateTimeOffset Later(DateTimeOffset? current, DateTimeOffset candidate) =>
        current is { } value && value > candidate ? value : candidate;
}

/// <summary>
/// What the history suggests, and what it could not explain.
/// <para>
/// <see cref="CatalogueGaps"/> is the most-trained few; <see cref="TotalCatalogueGaps"/> is how
/// many there are. A list that named all of them would be a wall rather than a report, and the
/// number is what says how big the gap is.
/// </para>
/// </summary>
public sealed record EquipmentSuggestions(
    IReadOnlyList<SuggestedEquipment> Suggestions,
    IReadOnlyList<CatalogueGap> CatalogueGaps,
    int TotalCatalogueGaps);

/// <summary>
/// One item the history implies, with the evidence for it. <see cref="Item"/> is a vocabulary
/// value, not display text — the frontend translates it (root standard 3).
/// </summary>
public sealed record SuggestedEquipment(
    string Item,
    string ImpliedByTitle,
    string ImpliedByExternalTemplateId,
    DateTimeOffset LastTrainedAt);

/// <summary>
/// A movement the user trained that our catalogue does not model.
/// <para>
/// The loud failure `TD-004` chose over a silent one, arriving as evidence rather than as a
/// guess. It implies no equipment, because we do not know what the exercise requires.
/// </para>
/// </summary>
public sealed record CatalogueGap(
    string ExternalTemplateId,
    string? Title,
    DateTimeOffset LastTrainedAt,
    int TimesTrained);

/// <summary>
/// An item the user was offered and refused.
/// <para>
/// Stored so it is not offered again on every sync (ADR-020). A suggestion that keeps returning
/// is one the user learns to dismiss without reading, which costs the feature its only job.
/// </para>
/// </summary>
public sealed class DeclinedEquipmentSuggestion
{
    public Guid Id { get; init; }

    public required string UserId { get; init; }

    public required EquipmentItem Item { get; init; }
}

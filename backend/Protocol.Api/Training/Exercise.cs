namespace Protocol.Api.Training;

/// <summary>
/// One performable exercise in our catalogue.
/// <para>
/// The catalogue is flat: <c>Preacher Curl (Barbell)</c>, <c>Preacher Curl (Dumbbell)</c> and
/// <c>Preacher Curl (Machine)</c> are three rows, not one movement with three children
/// (TD-015). The grouping a two-level model would add already exists as
/// <see cref="MovementPattern"/>, and the attribute a parent would most need to carry — the
/// muscle map — demonstrably changes by variant, so it cannot be inherited.
/// </para>
/// <para>
/// Our own <see cref="Id"/> identifies the exercise here; <see cref="ExternalTemplateId"/> is
/// how it maps to Hevy, and it is never a key (root standards 8 and 9, ADR-002).
/// </para>
/// </summary>
public sealed class Exercise
{
    public Guid Id { get; init; }

    /// <summary>
    /// Hevy's <c>exercise_template_id</c>, stored beside our own identifier rather than as one
    /// (root standard 8). Required: an exercise we cannot map is an exercise we cannot export.
    /// </summary>
    public required string ExternalTemplateId { get; init; }

    /// <summary>
    /// Display only, in English, shown as it arrives (root standard 9). Never matched, grouped,
    /// keyed or compared on — that is what keeps the history intact if titles are ever
    /// translated or reorganised.
    /// </summary>
    public required string Title { get; init; }

    public required MovementPattern MovementPattern { get; init; }

    public required Mechanic Mechanic { get; init; }

    public required Equipment Equipment { get; init; }

    public required OrderClass OrderClass { get; init; }

    public required Laterality Laterality { get; init; }

    /// <summary>
    /// The catalogue's draw order within a <see cref="MovementPattern"/> and
    /// <see cref="Equipment"/>. Selection needs a tie-break that is auditable rather than
    /// incidental to insertion order or identifier (ADR-005), and this is it.
    /// <para>
    /// What this rank may claim, and may not (TD-015): the catalogue prefers a variant because
    /// it is <b>performable and progressible in the assumed gym</b>, never because it produces
    /// more muscle. On growth the catalogue asserts nothing — every tested mechanism for one
    /// variant growing more than another has returned null.
    /// </para>
    /// </summary>
    public required int PreferenceRank { get; init; }

    /// <summary>The muscles this exercise loads, and how directly (TD-006).</summary>
    public ICollection<ExerciseMuscle> Muscles { get; init; } = [];
}

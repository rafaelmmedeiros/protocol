namespace Protocol.Api.Training;

/// <summary>
/// One thing an exercise needs to be performed at all.
/// <para>
/// An exercise is performable when its requirements are a **subset** of what the user owns. That
/// is the whole rule, and it is why this is a relation rather than a column: a bench press needs
/// a barbell and plates and a bench, and a model that can only name one of them offers movements
/// the user cannot do — the silent failure `TD-004` chose its assumption to avoid (`ADR-013`).
/// </para>
/// </summary>
public sealed class ExerciseRequirement
{
    public Guid ExerciseId { get; init; }

    public required EquipmentItem Item { get; init; }
}

/// <summary>
/// One thing a user has. No rows at all means the default — `TD-004`'s assumed gym — so a user
/// who never opens the equipment screen gets exactly the week `M1` would have given them.
/// </summary>
public sealed class UserEquipment
{
    public Guid Id { get; init; }

    public required string UserId { get; init; }

    public required EquipmentItem Item { get; init; }
}

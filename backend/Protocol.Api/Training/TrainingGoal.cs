namespace Protocol.Api.Training;

/// <summary>
/// What the user trains for.
/// <para>
/// More values exist than M1 programmes for, on purpose. ADR-004 collects the goal as a field
/// from the first migration — which is forward-only (root standard 10) — while M1 supports only
/// <see cref="Hypertrophy"/>. Every other value is accepted by the schema and rejected by the
/// API with <see cref="TrainingErrorCodes.GoalNotSupported"/>, rather than quietly programming
/// something no decision record covers.
/// </para>
/// </summary>
public enum TrainingGoal
{
    /// <summary>The only goal M1 programmes for (ADR-004, revision of 2026-08-22).</summary>
    Hypertrophy,

    Strength,
    WeightLoss,
    Endurance,
}

namespace Protocol.Api.Training;

/// <summary>
/// Trunk — the requirements and the rows for that part of the catalogue.
/// <para>
/// Split out under ADR-023: the catalogue stays authored in C#, and one file per group is what
/// keeps a 60-row hand-curated table readable. Both members are <b>methods</b>, not field
/// initialisers, on purpose: C# does not define the order static field initialisers run in
/// across the files of a partial class, and <see cref="ExerciseCatalogue.All"/> reads every
/// requirement while it builds. A method has no such order to get wrong.
/// </para>
/// </summary>
public static partial class ExerciseCatalogue
{
    private static KeyValuePair<string, EquipmentItem[]>[] TrunkRequirements() =>
    [
        // Trunk
        new("23A48484", [EquipmentItem.CableStation]),
        new("DCF3B31B", [EquipmentItem.Bodyweight]),
        new("CC55119B", [EquipmentItem.CableStation]),

        // M4. Rows curated from what a real account actually logged. Machine items are named
        // individually because that is the unit a gym has or does not have (ADR-022).
        new("EB43ADD4", [EquipmentItem.AbdominalMachine]),
    ];

    private static Exercise[] Trunk() =>
    [
        // ---- Trunk -----------------------------------------------------------------------
        Make("23A48484", "Cable Crunch", MovementPattern.TrunkFlexion, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Abs),

        Make("DCF3B31B", "Crunch", MovementPattern.TrunkFlexion, Mechanic.Isolation,
            Equipment.Bodyweight, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Abs),

        Make("CC55119B", "Cable Core Pallof Press", MovementPattern.AntiRotation, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Unilateral, 1,
            MuscleGroup.Abs),

        // ---- M4 ----------------------------------------------------------------------
        // Curated from what a real account actually logged, ordered by how often each
        // movement was trained. These rows are not all performable in TD-004's assumed gym,
        // and under TD-019 they no longer have to be.

        Make("EB43ADD4", "Crunch (Machine)", MovementPattern.TrunkFlexion, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 3,
            MuscleGroup.Abs),
    ];
}

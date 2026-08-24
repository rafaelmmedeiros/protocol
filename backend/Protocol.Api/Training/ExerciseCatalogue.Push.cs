namespace Protocol.Api.Training;

/// <summary>
/// Upper body, push — the requirements and the rows for that part of the catalogue.
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
    private static KeyValuePair<string, EquipmentItem[]>[] PushRequirements() =>
    [
        // Upper body, push
        new("79D0BB3A", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench]),
        new("50DFDFAB", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench, EquipmentItem.AdjustableBench]),
        new("3601968B", [EquipmentItem.Dumbbells, EquipmentItem.Bench]),
        new("07B38369", [EquipmentItem.Dumbbells, EquipmentItem.Bench, EquipmentItem.AdjustableBench]),
        new("7B8D84E8", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),
        new("6AC96645", [EquipmentItem.Dumbbells]),
        new("651F844C", [EquipmentItem.CableStation]),
        new("12017185", [EquipmentItem.Dumbbells, EquipmentItem.Bench]),
        new("422B08F1", [EquipmentItem.Dumbbells]),
        new("BE289E45", [EquipmentItem.CableStation]),
        new("93A552C6", [EquipmentItem.CableStation]),
        new("3765684D", [EquipmentItem.Dumbbells]),

        // M4. Rows curated from what a real account actually logged. Machine items are named
        // individually because that is the unit a gym has or does not have (ADR-022).
        new("7EB3F7C3", [EquipmentItem.ChestPressMachine]),
        new("78683336", [EquipmentItem.PecDeckMachine]),
        new("9237BAD1", [EquipmentItem.ShoulderPressMachine]),
        new("878CD1D0", [EquipmentItem.Dumbbells, EquipmentItem.AdjustableBench]),
        new("35B51B87", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench]),
    ];

    private static Exercise[] Push() =>
    [
        // ---- Upper body, push ------------------------------------------------------------
        Make("79D0BB3A", "Bench Press (Barbell)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.Chest, MuscleGroup.Triceps, MuscleGroup.FrontDelts),

        Make("50DFDFAB", "Incline Bench Press (Barbell)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 2,
            MuscleGroup.Chest, MuscleGroup.FrontDelts, MuscleGroup.Triceps),

        Make("3601968B", "Bench Press (Dumbbell)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.Chest, MuscleGroup.Triceps, MuscleGroup.FrontDelts),

        Make("07B38369", "Incline Bench Press (Dumbbell)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 2,
            MuscleGroup.Chest, MuscleGroup.FrontDelts, MuscleGroup.Triceps),

        Make("7B8D84E8", "Overhead Press (Barbell)", MovementPattern.VerticalPush, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.FrontDelts, MuscleGroup.Triceps, MuscleGroup.SideDelts),

        Make("6AC96645", "Overhead Press (Dumbbell)", MovementPattern.VerticalPush, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.FrontDelts, MuscleGroup.Triceps, MuscleGroup.SideDelts),

        Make("651F844C", "Cable Fly Crossovers", MovementPattern.HorizontalAdduction, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Chest),

        Make("12017185", "Chest Fly (Dumbbell)", MovementPattern.HorizontalAdduction, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Chest),

        Make("422B08F1", "Lateral Raise (Dumbbell)", MovementPattern.LateralRaise, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.SideDelts),

        Make("BE289E45", "Lateral Raise (Cable)", MovementPattern.LateralRaise, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Unilateral, 1,
            MuscleGroup.SideDelts),

        Make("93A552C6", "Triceps Pushdown", MovementPattern.ElbowExtension, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Triceps),

        Make("3765684D", "Triceps Extension (Dumbbell)", MovementPattern.ElbowExtension, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Triceps),

        // ---- M4 ----------------------------------------------------------------------
        // Curated from what a real account actually logged, ordered by how often each
        // movement was trained. These rows are not all performable in TD-004's assumed gym,
        // and under TD-019 they no longer have to be.

        Make("7EB3F7C3", "Chest Press (Machine)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundSecondary, Laterality.Bilateral, 5,
            MuscleGroup.Chest, MuscleGroup.Triceps, MuscleGroup.FrontDelts),

        // Triceps primary, which is the whole reason it earns a row beside the flat bench press:
        // same pattern, same implement, same requirements, different thing loaded.
        Make("35B51B87", "Bench Press - Close Grip (Barbell)", MovementPattern.HorizontalPush, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 6,
            MuscleGroup.Triceps, MuscleGroup.Chest, MuscleGroup.FrontDelts),

        // A pec deck runs forwards for a fly and backwards for a rear delt fly (see Pull),
        // so the two rows share a requirement and differ only in what they load.
        Make("78683336", "Chest Fly (Machine)", MovementPattern.HorizontalAdduction, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 3,
            MuscleGroup.Chest),

        Make("9237BAD1", "Seated Shoulder Press (Machine)", MovementPattern.VerticalPush, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundSecondary, Laterality.Bilateral, 4,
            MuscleGroup.FrontDelts, MuscleGroup.Triceps, MuscleGroup.SideDelts),

        // Seated, and that is the whole difference: it needs a bench with a back where the
        // standing row needs only dumbbells. Not because it is more stable -- TD-005 omitted
        // stability_demand, and reversing that omission would be a new record rather than a row.
        Make("878CD1D0", "Shoulder Press (Dumbbell)", MovementPattern.VerticalPush, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 3,
            MuscleGroup.FrontDelts, MuscleGroup.Triceps, MuscleGroup.SideDelts),
    ];
}

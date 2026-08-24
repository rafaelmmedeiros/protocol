namespace Protocol.Api.Training;

/// <summary>
/// Lower body — the requirements and the rows for that part of the catalogue.
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
    private static KeyValuePair<string, EquipmentItem[]>[] LowerBodyRequirements() =>
    [
        // Lower body
        new("D04AC939", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.SquatRack]),
        new("6622E5A0", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.SquatRack]),
        new("2B4B7310", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),
        new("C6272009", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),
        new("72CFFAD5", [EquipmentItem.Dumbbells]),
        new("B5D3A742", [EquipmentItem.Dumbbells, EquipmentItem.Bench]),
        new("20C1A3CB", [EquipmentItem.Dumbbells]),
        new("D57C2EC7", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench]),
        new("8C331CD8", [EquipmentItem.CableStation]),
        new("6DA40660", [EquipmentItem.Dumbbells]),
        new("E53CCBE5", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),

        // M4. Rows curated from what a real account actually logged. Machine items are named
        // individually because that is the unit a gym has or does not have (ADR-022): a seated
        // leg curl and a lying leg curl are separate purchases, and collapsing them would
        // prescribe the one that is missing.
        new("11A123F3", [EquipmentItem.SeatedLegCurlMachine]),
        new("B8127AD1", [EquipmentItem.LyingLegCurlMachine]),
        new("6120CAAB", [EquipmentItem.StandingLegCurlMachine]),
        new("75A4F6C4", [EquipmentItem.LegExtensionMachine]),
        new("C7973E0E", [EquipmentItem.LegPressMachine]),
        new("1E42FD5F", [EquipmentItem.HackSquatMachine]),
        new("CC35A01F", [EquipmentItem.SmithMachine, EquipmentItem.WeightPlates]),
        new("F4B4C6EE", [EquipmentItem.HipAbductionMachine]),
        new("062AB91A", [EquipmentItem.SeatedCalfRaiseMachine]),
        new("E05C2C38", [EquipmentItem.StandingCalfRaiseMachine]),
        new("4F5866F8", [EquipmentItem.BackExtensionBench]),
        new("091737FA", [EquipmentItem.BackExtensionBench, EquipmentItem.WeightPlates]),
        new("06745E58", [EquipmentItem.Bodyweight]),
    ];

    private static Exercise[] LowerBody() =>
    [
        // ---- Lower body ------------------------------------------------------------------
        Make("D04AC939", "Squat (Barbell)", MovementPattern.Squat, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.SpinalErectors, MuscleGroup.Adductors),

        Make("6622E5A0", "Sumo Squat (Barbell)", MovementPattern.Squat, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 2,
            MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Adductors),

        Make("2B4B7310", "Romanian Deadlift (Barbell)", MovementPattern.Hinge, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.SpinalErectors),

        Make("C6272009", "Deadlift (Barbell)", MovementPattern.Hinge, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 2,
            MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Quads, MuscleGroup.SpinalErectors),

        Make("72CFFAD5", "Romanian Deadlift (Dumbbell)", MovementPattern.Hinge, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.SpinalErectors),

        Make("B5D3A742", "Bulgarian Split Squat (Dumbbell)", MovementPattern.Lunge, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Unilateral, 1,
            MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Adductors),

        Make("20C1A3CB", "Split Squat (Dumbbell)", MovementPattern.Lunge, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Unilateral, 2,
            MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Adductors),

        Make("D57C2EC7", "Hip Thrust (Barbell)", MovementPattern.HipExtension, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.Glutes, MuscleGroup.Hamstrings),

        Make("8C331CD8", "Cable Pull Through", MovementPattern.HipExtension, Mechanic.Compound,
            Equipment.Cable, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.Glutes, MuscleGroup.Hamstrings),

        Make("6DA40660", "Standing Calf Raise (Dumbbell)", MovementPattern.CalfRaise, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Calves),

        Make("E53CCBE5", "Standing Calf Raise (Barbell)", MovementPattern.CalfRaise, Mechanic.Isolation,
            Equipment.Barbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Calves),

        // ---- M4 ----------------------------------------------------------------------
        // Curated from what a real account actually logged, ordered by how often each
        // movement was trained. These rows are not all performable in TD-004's assumed gym,
        // and under TD-019 they no longer have to be.

        // Knee flexion -- the hole TD-004 named in M1 and TD-019 still carries. Calves are left
        // off deliberately: gastrocnemius crosses the knee but is not meaningfully loaded through
        // a substantial range here, which is the line TD-005 draws.
        Make("11A123F3", "Seated Leg Curl (Machine)", MovementPattern.KneeFlexion, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Hamstrings),
        Make("B8127AD1", "Lying Leg Curl (Machine)", MovementPattern.KneeFlexion, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 2,
            MuscleGroup.Hamstrings),
        Make("6120CAAB", "Standing Leg Curl (Machine)", MovementPattern.KneeFlexion, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Unilateral, 3,
            MuscleGroup.Hamstrings),

        Make("75A4F6C4", "Leg Extension (Machine)", MovementPattern.KneeExtension, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Quads),

        // Glutes rather than a separate abductor group: gluteus medius is a glute, and TD-005's
        // vocabulary does not split it. Naming a value to carry one row would be inventing one.
        Make("F4B4C6EE", "Hip Abduction (Machine)", MovementPattern.HipAbduction, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Glutes),

        // Hamstrings are left off all three: they lengthen at the hip and shorten at the knee at
        // once, so the range is not substantial. The reason TD-005 gives for excluding erectors
        // on a leg press is the same reason.
        Make("C7973E0E", "Leg Press (Machine)", MovementPattern.Squat, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundPrimary, Laterality.Bilateral, 2,
            MuscleGroup.Quads, MuscleGroup.Glutes),
        Make("1E42FD5F", "Hack Squat (Machine)", MovementPattern.Squat, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundPrimary, Laterality.Bilateral, 3,
            MuscleGroup.Quads, MuscleGroup.Glutes),
        Make("CC35A01F", "Squat (Smith Machine)", MovementPattern.Squat, Mechanic.Compound,
            Equipment.SmithMachine, OrderClass.CompoundPrimary, Laterality.Bilateral, 4,
            MuscleGroup.Quads, MuscleGroup.Glutes),

        Make("062AB91A", "Seated Calf Raise (Machine)", MovementPattern.CalfRaise, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Calves),
        Make("E05C2C38", "Standing Calf Raise (Machine)", MovementPattern.CalfRaise, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 2,
            MuscleGroup.Calves),
        Make("06745E58", "Standing Calf Raise", MovementPattern.CalfRaise, Mechanic.Isolation,
            Equipment.Bodyweight, OrderClass.Isolation, Laterality.Bilateral, 5,
            MuscleGroup.Calves),

        // Loaded and unloaded are separate rows because they require different things: the second
        // needs plates and the first needs nothing but the bench.
        Make("4F5866F8", "Back Extension (Hyperextension)", MovementPattern.HipExtension, Mechanic.Compound,
            Equipment.Bodyweight, OrderClass.Isolation, Laterality.Bilateral, 3,
            MuscleGroup.SpinalErectors, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
        Make("091737FA", "Back Extension (Weighted Hyperextension)", MovementPattern.HipExtension, Mechanic.Compound,
            Equipment.BodyweightLoadable, OrderClass.Isolation, Laterality.Bilateral, 4,
            MuscleGroup.SpinalErectors, MuscleGroup.Glutes, MuscleGroup.Hamstrings),
    ];
}

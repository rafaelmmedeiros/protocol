namespace Protocol.Api.Training;

/// <summary>
/// Upper body, pull — the requirements and the rows for that part of the catalogue.
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
    private static KeyValuePair<string, EquipmentItem[]>[] PullRequirements() =>
    [
        // Upper body, pull
        new("1B2B1E7C", [EquipmentItem.PullUpBar]),
        new("6A6C31A5", [EquipmentItem.LatPulldownStation]),
        new("55E6546F", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),
        new("0393F233", [EquipmentItem.CableStation]),
        new("23E92538", [EquipmentItem.Dumbbells]),
        new("B582299E", [EquipmentItem.Dumbbells, EquipmentItem.Bench, EquipmentItem.AdjustableBench]),
        new("A5AC6449", [EquipmentItem.Barbell, EquipmentItem.WeightPlates]),
        new("4F942934", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench, EquipmentItem.AdjustableBench]),
        new("37FCC2BB", [EquipmentItem.Dumbbells]),
        new("ADA8623C", [EquipmentItem.CableStation]),

        // M4. Rows curated from what a real account actually logged. Machine items are named
        // individually because that is the unit a gym has or does not have (ADR-022).
        new("D8281C62", [EquipmentItem.PecDeckMachine]),
        new("1DF4A847", [EquipmentItem.SeatedRowMachine]),
        new("BC3492DA", [EquipmentItem.HighRowMachine]),
        new("1E9A6B8E", [EquipmentItem.PreacherCurlMachine]),
        new("95F2E076", [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench]),
        new("D8460FA6", [EquipmentItem.Dumbbells, EquipmentItem.Bench]),
        new("8BAB2735", [EquipmentItem.Dumbbells, EquipmentItem.AdjustableBench]),
        new("D2387AB1", [EquipmentItem.CableStation]),
    ];

    private static Exercise[] Pull() =>
    [
        // ---- Upper body, pull ------------------------------------------------------------
        Make("1B2B1E7C", "Pull Up", MovementPattern.VerticalPull, Mechanic.Compound,
            Equipment.BodyweightLoadable, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.Lats, MuscleGroup.UpperBack, MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("6A6C31A5", "Lat Pulldown (Cable)", MovementPattern.VerticalPull, Mechanic.Compound,
            Equipment.Cable, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.Lats, MuscleGroup.UpperBack, MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("55E6546F", "Bent Over Row (Barbell)", MovementPattern.HorizontalPull, Mechanic.Compound,
            Equipment.Barbell, OrderClass.CompoundPrimary, Laterality.Bilateral, 1,
            MuscleGroup.UpperBack, MuscleGroup.Lats, MuscleGroup.Biceps, MuscleGroup.Forearms,
            MuscleGroup.SpinalErectors),

        Make("0393F233", "Seated Cable Row - V Grip (Cable)", MovementPattern.HorizontalPull, Mechanic.Compound,
            Equipment.Cable, OrderClass.CompoundSecondary, Laterality.Bilateral, 1,
            MuscleGroup.UpperBack, MuscleGroup.Lats, MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("23E92538", "Bent Over Row (Dumbbell)", MovementPattern.HorizontalPull, Mechanic.Compound,
            Equipment.Dumbbell, OrderClass.CompoundSecondary, Laterality.Unilateral, 1,
            MuscleGroup.UpperBack, MuscleGroup.Lats, MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("B582299E", "Chest Supported Reverse Fly (Dumbbell)", MovementPattern.HorizontalAbduction,
            Mechanic.Isolation, Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.RearDelts, MuscleGroup.UpperBack),

        Make("A5AC6449", "Bicep Curl (Barbell)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Barbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("4F942934", "Preacher Curl (Barbell)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Barbell, OrderClass.Isolation, Laterality.Bilateral, 2,
            MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("37FCC2BB", "Bicep Curl (Dumbbell)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Biceps, MuscleGroup.Forearms),

        Make("ADA8623C", "Bicep Curl (Cable)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Biceps, MuscleGroup.Forearms),

        // ---- M4 ----------------------------------------------------------------------
        // Curated from what a real account actually logged, ordered by how often each
        // movement was trained. These rows are not all performable in TD-004's assumed gym,
        // and under TD-019 they no longer have to be.

        // The pec deck again, run backwards (see Push). Same requirement, different load.
        Make("D8281C62", "Rear Delt Reverse Fly (Machine)", MovementPattern.HorizontalAbduction, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 2,
            MuscleGroup.RearDelts, MuscleGroup.UpperBack),

        Make("1DF4A847", "Seated Row (Machine)", MovementPattern.HorizontalPull, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundSecondary, Laterality.Bilateral, 4,
            MuscleGroup.UpperBack, MuscleGroup.Lats, MuscleGroup.Biceps),
        Make("BC3492DA", "Iso-Lateral High Row (Machine)", MovementPattern.VerticalPull, Mechanic.Compound,
            Equipment.Machine, OrderClass.CompoundSecondary, Laterality.Unilateral, 3,
            MuscleGroup.Lats, MuscleGroup.UpperBack, MuscleGroup.Biceps),

        // Triceps rather than biceps as the secondary: the elbow does not move, so the long head
        // resists shoulder flexion through the whole range while the biceps do nothing.
        Make("D2387AB1", "Straight Arm Lat Pulldown (Cable)", MovementPattern.VerticalPull, Mechanic.Isolation,
            Equipment.Cable, OrderClass.Isolation, Laterality.Bilateral, 4,
            MuscleGroup.Lats, MuscleGroup.Triceps),

        Make("1E9A6B8E", "Preacher Curl (Machine)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Machine, OrderClass.Isolation, Laterality.Bilateral, 5,
            MuscleGroup.Biceps),
        Make("8BAB2735", "Seated Incline Curl (Dumbbell)", MovementPattern.ElbowFlexion, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 6,
            MuscleGroup.Biceps),

        // A hammer curl, a reverse curl and a cross body hammer curl are absent, and the absence
        // is deliberate enough to spend a comment on. In everything this model represents they
        // are the dumbbell and barbell curls already above: same pattern, same implement, same
        // requirements, and M1 tags Forearms secondary on every curl. Grip is the only thing left
        // separating them, and TD-005 omitted grip.
        //
        // Narrowing the supinated curls' attribution to make room for them was proposed and
        // rejected on the evidence, not on taste (TD-020). They stay in the coverage report as
        // movements this model does not represent, which is the honest place for them.
        //
        // The wrist curls below are the answer to the underlying want. They train the forearm
        // through a joint action no curl reaches at any grip, so they earn rows without anything
        // being re-decided.
        Make("95F2E076", "Seated Wrist Curl (Barbell)", MovementPattern.WristFlexion, Mechanic.Isolation,
            Equipment.Barbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Forearms),

        // The extensor side. A separate row rather than a variant of the one above, because the
        // muscles on the back of the forearm are not the ones on the front — the same reason a
        // leg extension is not a leg curl.
        Make("D8460FA6", "Reverse Wrist Curl (Dumbbell)", MovementPattern.WristExtension, Mechanic.Isolation,
            Equipment.Dumbbell, OrderClass.Isolation, Laterality.Bilateral, 1,
            MuscleGroup.Forearms),
    ];
}

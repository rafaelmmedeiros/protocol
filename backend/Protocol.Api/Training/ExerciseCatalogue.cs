namespace Protocol.Api.Training;

/// <summary>
/// The seeded exercise catalogue.
/// <para>
/// Curated by hand, and it has to be. Hevy supplies <c>exercise_template_id</c> and nothing
/// else usable: it encodes the variant only in the title string (which root standard 9 forbids
/// us parsing), collapses cable, Smith machine and selectorised machine into one
/// <c>machine</c> value, collapses all three deltoid heads into <c>shoulders</c>, and leaves
/// <c>secondary_muscle_groups</c> empty on most isolation templates. Every attribute below is
/// therefore ours (ADR-002, TD-015).
/// </para>
/// <para>
/// Scoped to TD-004's assumed gym: barbell, plates, rack, adjustable bench; dumbbells; an
/// adjustable cable station with a lat pulldown; a pull-up bar. <b>No selectorised machines</b>
/// — so there is no direct <see cref="MovementPattern.KneeFlexion"/> exercise here, exactly the
/// hole TD-004 names rather than hides.
/// </para>
/// </summary>
public static class ExerciseCatalogue
{
    /// <summary>
    /// What each exercise needs to be performed at all, keyed by Hevy's template id because that
    /// is the one identifier stable across a re-seed (our own key is generated).
    /// <para>
    /// Kept as a table rather than a parameter on every row so the whole gym is readable in one
    /// place — this is the list to check against a real gym, and against `TD-004`'s assumption.
    /// A row missing from here throws at startup rather than seeding an exercise nobody can
    /// perform (ADR-013).
    /// </para>
    /// <para>
    /// Two judgements worth arguing with. A bench press requires a <c>Bench</c> and not a
    /// <c>SquatRack</c>, because a bench with uprights is the ordinary case. And a preacher curl
    /// requires an <c>AdjustableBench</c> rather than a preacher bench of its own — an adjustable
    /// bench set upright serves, which keeps `TD-004`'s assumed gym intact instead of quietly
    /// widening it.
    /// </para>
    /// <para>
    /// <b>Declared before <see cref="All"/> on purpose.</b> Static field initialisers run in
    /// textual order, and <see cref="All"/> reads this while building every row — below it, this
    /// dictionary is still null and the type initialiser throws.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, EquipmentItem[]> Requirements = new()
    {
        // Lower body
        ["D04AC939"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.SquatRack],
        ["6622E5A0"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.SquatRack],
        ["2B4B7310"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],
        ["C6272009"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],
        ["72CFFAD5"] = [EquipmentItem.Dumbbells],
        ["B5D3A742"] = [EquipmentItem.Dumbbells, EquipmentItem.Bench],
        ["20C1A3CB"] = [EquipmentItem.Dumbbells],
        ["D57C2EC7"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench],
        ["8C331CD8"] = [EquipmentItem.CableStation],
        ["6DA40660"] = [EquipmentItem.Dumbbells],
        ["E53CCBE5"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],

        // Upper body, push
        ["79D0BB3A"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench],
        ["50DFDFAB"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench, EquipmentItem.AdjustableBench],
        ["3601968B"] = [EquipmentItem.Dumbbells, EquipmentItem.Bench],
        ["07B38369"] = [EquipmentItem.Dumbbells, EquipmentItem.Bench, EquipmentItem.AdjustableBench],
        ["7B8D84E8"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],
        ["6AC96645"] = [EquipmentItem.Dumbbells],
        ["651F844C"] = [EquipmentItem.CableStation],
        ["12017185"] = [EquipmentItem.Dumbbells, EquipmentItem.Bench],
        ["422B08F1"] = [EquipmentItem.Dumbbells],
        ["BE289E45"] = [EquipmentItem.CableStation],
        ["93A552C6"] = [EquipmentItem.CableStation],
        ["3765684D"] = [EquipmentItem.Dumbbells],

        // Upper body, pull
        ["1B2B1E7C"] = [EquipmentItem.PullUpBar],
        ["6A6C31A5"] = [EquipmentItem.LatPulldownStation],
        ["55E6546F"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],
        ["0393F233"] = [EquipmentItem.CableStation],
        ["23E92538"] = [EquipmentItem.Dumbbells],
        ["B582299E"] = [EquipmentItem.Dumbbells, EquipmentItem.Bench, EquipmentItem.AdjustableBench],
        ["A5AC6449"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates],
        ["4F942934"] = [EquipmentItem.Barbell, EquipmentItem.WeightPlates, EquipmentItem.Bench, EquipmentItem.AdjustableBench],
        ["37FCC2BB"] = [EquipmentItem.Dumbbells],
        ["ADA8623C"] = [EquipmentItem.CableStation],

        // Trunk
        ["23A48484"] = [EquipmentItem.CableStation],
        ["DCF3B31B"] = [EquipmentItem.Bodyweight],
        ["CC55119B"] = [EquipmentItem.CableStation],
    };

    /// <summary>
    /// The gym `TD-004` assumed, expressed as items. A user with no equipment rows is treated as
    /// having exactly this, so a user who never opens the screen gets `M1`'s week unchanged.
    /// </summary>
    public static IReadOnlySet<EquipmentItem> AssumedGym { get; } = new HashSet<EquipmentItem>
    {
        EquipmentItem.Bodyweight,
        EquipmentItem.Barbell,
        EquipmentItem.WeightPlates,
        EquipmentItem.Dumbbells,
        EquipmentItem.Bench,
        EquipmentItem.AdjustableBench,
        EquipmentItem.SquatRack,
        EquipmentItem.PullUpBar,
        EquipmentItem.CableStation,
        EquipmentItem.LatPulldownStation,
    };

    /// <summary>
    /// Every exercise M1 can prescribe. Ordering within this list is irrelevant: selection
    /// draws on <see cref="Exercise.PreferenceRank"/>, never on insertion order, identifier or
    /// title (TD-005, ADR-005).
    /// </summary>
    public static IReadOnlyList<Exercise> All { get; } =
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
    ];

    /// <summary>
    /// Builds one catalogue row. <paramref name="secondary"/> is the judgement call TD-005 names
    /// as the soft spot of the whole design: it means "meaningfully loaded through a substantial
    /// range", not "anything that contracts". Grip on a deadlift and erectors on a leg press are
    /// both excluded by that rule. Tagged inconsistently, every volume number moves and no diff
    /// shows it.
    /// </summary>
    private static Exercise Make(
        string externalTemplateId,
        string title,
        MovementPattern movementPattern,
        Mechanic mechanic,
        Equipment equipment,
        OrderClass orderClass,
        Laterality laterality,
        int preferenceRank,
        MuscleGroup primary,
        params MuscleGroup[] secondary) => new()
        {
            Id = Guid.CreateVersion7(),
            ExternalTemplateId = externalTemplateId,
            Requirements =
            [
                .. (Requirements.TryGetValue(externalTemplateId, out var items)
                        ? items
                        : throw new InvalidOperationException(
                            $"'{title}' has no equipment requirements. Every exercise needs at least "
                            + "one (ADR-013); a bodyweight movement requires EquipmentItem.Bodyweight."))
                    .Select(item => new ExerciseRequirement { Item = item }),
            ],
            Title = title,
            MovementPattern = movementPattern,
            Mechanic = mechanic,
            Equipment = equipment,
            OrderClass = orderClass,
            Laterality = laterality,
            PreferenceRank = preferenceRank,
            Muscles =
            [
                new ExerciseMuscle { MuscleGroup = primary, Role = MuscleRole.Primary },
                .. secondary.Select(m => new ExerciseMuscle { MuscleGroup = m, Role = MuscleRole.Secondary }),
            ],
        };
}

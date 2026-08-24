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
/// <b>Not scoped to any one gym (TD-019).</b> It models the movements this product reasons
/// about, selectorised machines included, whether or not a given user can perform them; what a
/// user may be drawn is decided by their equipment set at draw time (ADR-013), never by what the
/// seed contains. <see cref="AssumedGym"/> is the thing that stays lean, and it is a different
/// list — a row here is not a claim that a stranger owns the machine it needs.
/// </para>
/// <para>
/// A movement earns a row when it differs from every existing one in its movement pattern, its
/// implement, the equipment it requires, or what it loads — never in its title (standard 9), and
/// never in an attribute TD-005 deliberately omitted. That is the whole admission rule, and it is
/// why a close grip earns a row (triceps primary) and a rope attachment does not.
/// </para>
/// </summary>
public static partial class ExerciseCatalogue
{
    /// <summary>
    /// What each exercise needs to be performed at all, keyed by Hevy's template id because that
    /// is the one identifier stable across a re-seed (our own key is generated).
    /// <para>
    /// Kept as a table rather than a parameter on every row so the whole gym is readable in one
    /// place — this is the list to check against a real gym.
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
    private static readonly Dictionary<string, EquipmentItem[]> Requirements =
        ((KeyValuePair<string, EquipmentItem[]>[])
        [
            .. LowerBodyRequirements(),
            .. PushRequirements(),
            .. PullRequirements(),
            .. TrunkRequirements(),
        ]).ToDictionary();

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
        .. LowerBody(),
        .. Push(),
        .. Pull(),
        .. Trunk(),
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

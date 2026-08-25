namespace Protocol.Api.Training;

/// <summary>
/// What the user trains for and what they have available. One per user.
/// <para>
/// Three fields, decided last on purpose (ADR-004). There is no experience level: it is a proxy
/// for how someone responds to a stimulus, which this system will observe rather than ask about
/// — and TD-001 records that it observes nothing yet and starts everyone conservative.
/// </para>
/// <para>
/// There is no rest column either. Rest between sets is a property of the slot, not of the
/// person, and the record decides it rather than the user (ADR-007, TD-011).
/// </para>
/// </summary>
public sealed class TrainingProfile
{
    public Guid Id { get; init; }

    /// <summary>The owning Identity user. One profile per user.</summary>
    public required string UserId { get; init; }

    public required TrainingGoal Goal { get; set; }

    /// <summary>Sessions per week. Supported range is decided by TD-002.</summary>
    public required int DaysPerWeek { get; set; }

    /// <summary>
    /// Which template this frequency is run with, or <c>null</c> for whatever it maps to
    /// (TD-023, ADR-030).
    /// <para>
    /// <b>Null carries information and is not an unset field.</b> It distinguishes a user who
    /// never chose from one who chose the default, which is what makes a future change to the
    /// mapping safe: the first should follow the new mapping and the second must not be moved
    /// silently. Writing the mapped value at creation would destroy that distinction
    /// permanently, and it is not recoverable afterwards.
    /// </para>
    /// </summary>
    public SplitTemplateId? Split { get; set; }

    /// <summary>
    /// How long a session may last, in seconds — the unit is in the field name (root
    /// standard 4). Minutes exist only in what a user sees; the domain never holds them.
    /// Supported range is decided by TD-012.
    /// </summary>
    public required int SessionDurationSeconds { get; set; }
}

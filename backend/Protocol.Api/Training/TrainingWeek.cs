namespace Protocol.Api.Training;

/// <summary>
/// The calendar week, as the unit every analysis measures over.
/// <para>
/// <b>This is what survived `ADR-027`, and the distinction is the whole point of that record.</b>
/// A plan stopped being a calendar week — it is an ordered queue with no dates — but what was
/// *performed* is still bucketed into Monday-anchored weeks, because that is a measurement
/// convention and not a shape the prescription has to take. Root standard 6 constrains this
/// file and says nothing about the queue.
/// </para>
/// <para>
/// Never derived from locale. An <c>en-US</c> week starting on Sunday must not redraw the
/// boundaries of a training block that already exists.
/// </para>
/// </summary>
public static class TrainingWeek
{
    /// <summary>Monday is zero, Sunday is six — the training week's own order, not the locale's.</summary>
    public static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7; // root standard 6

    /// <summary>The Monday of the week a date falls in.</summary>
    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-DaysFromMonday(date.DayOfWeek));

    /// <summary>
    /// The Monday of the week an instant falls in, read in UTC. Training is stored in UTC and
    /// localised only at the render edge (root standard 5), so bucketing reads the same instant
    /// everywhere rather than shifting a session between weeks by timezone.
    /// </summary>
    public static DateOnly MondayOf(DateTimeOffset instant) =>
        MondayOf(DateOnly.FromDateTime(instant.UtcDateTime));
}

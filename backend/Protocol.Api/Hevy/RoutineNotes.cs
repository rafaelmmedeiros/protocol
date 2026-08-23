using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// The one line of text a pushed routine carries per exercise (ADR-016).
/// <para>
/// **This is the only display text the backend composes**, and it is here rather than in the
/// frontend because it is written to a third party rather than returned to us — root standard 3
/// governs what the API answers our own frontend with, and this crosses a different edge. The
/// push carries the user's locale so the sentence is in their language (root standard 2).
/// </para>
/// <para>
/// It exists because a Hevy routine set has no effort field: <c>rpe</c> is feedback, produced
/// after a set, and a plan does not carry an observation. A note is the only channel, and the
/// experiment that proved routine notes do **not** survive into the workout is what makes it
/// safe — nothing can later mistake this for data and read it back.
/// </para>
/// <para>
/// The wording matters more than it looks. It frames the range as something to **terminate on
/// effort within**, not a number to reach, because a fixed target censors the observation: a
/// lifter who stops at twelve because the plan says twelve cannot log more than twelve, and the
/// count then carries nothing about the effort behind it. This sentence is the only intervention
/// found that makes the imported log more informative without asking the user anything.
/// </para>
/// </summary>
public static class RoutineNotes
{
    /// <summary>The default, and the product's default locale (root standard 2).</summary>
    public const string DefaultLocale = "en-US";

    /// <summary>The locales the product supports. Anything else falls back rather than failing.</summary>
    public static readonly IReadOnlySet<string> SupportedLocales =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en-US", "pt-BR" };

    public static string For(GeneratedPrescription prescription, string? locale) =>
        Normalise(locale) switch
        {
            "pt-BR" =>
                $"{prescription.MinReps}-{prescription.MaxReps} reps — pare quando o movimento "
                + $"perder velocidade, com cerca de {prescription.RepsInReserve} na reserva. "
                + "A faixa descreve onde a série deve cair, não um número a perseguir.",
            _ =>
                $"{prescription.MinReps}-{prescription.MaxReps} reps — stop when the movement "
                + $"slows, with about {prescription.RepsInReserve} left in reserve. "
                + "The range describes where the set should land, not a number to chase.",
        };

    private static string Normalise(string? locale) =>
        locale is not null && SupportedLocales.Contains(locale)
            ? SupportedLocales.First(supported => supported.Equals(locale, StringComparison.OrdinalIgnoreCase))
            : DefaultLocale;
}

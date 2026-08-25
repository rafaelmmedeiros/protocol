import { Card, CardHeader } from "@/components/ui/card";
import { Pill, type Tone } from "@/components/ui/pill";

export type PerformedSet = {
  position: number;
  weightKg: number | null;
  reps: number | null;
  /** Null means the user reported nothing, and never means they had nothing left (TD-017). */
  repsInReserve: number | null;
};

export type Slot = {
  prescriptionId: string;
  exerciseTitle: string | null;
  externalTemplateId: string | null;
  prescribedSets: number;
  minReps: number;
  maxReps: number;
  repsInReserve: number;
  restSeconds: number;
  performedSets: PerformedSet[];
  outcome: string;
};

export type Extra = {
  externalTemplateId: string;
  title: string | null;
  exerciseId: string | null;
  sets: PerformedSet[];
};

export type SessionComparison = {
  position: number;
  /** Null since ADR-027; a queue position labels the session instead. */
  day: string | null;
  kind: string;
  performed: boolean;
  performedAt: string | null;
  slots: Slot[];
  extras: Extra[];
};

export type Comparison = {
  weekId: string;
  weekStartDate: string | null;
  sessions: SessionComparison[];
  unboundWorkouts: { externalWorkoutId: string; startedAt: string; exerciseCount: number }[];
  coverage: { importedWorkouts: number; boundWorkouts: number };
};

export type ComparisonStrings = {
  title: string;
  lead: string;
  prescribed: string;
  performed: string;
  notPerformed: string;
  noEffortReported: string;
  outcomes: Record<string, string>;
  extrasTitle: string;
  extrasLead: string;
  unboundTitle: string;
  unboundLead: string;
  coverage: string;
  /** Already interpolated per row: the count is exercises, not sets. */
  exerciseCount: (n: number) => string;
};

/** Colour is never the only signal — the pill carries a dot and a word as well. */
const TONES: Record<string, Tone> = {
  InRange: "ok",
  AboveRange: "accent",
  BelowRange: "warn",
  Mixed: "warn",
  NotPerformed: "neutral",
};

/** The performed sequence, as one line. Ordered, and never summed. */
function Sequence({ sets, noEffortReported }: { sets: PerformedSet[]; noEffortReported: string }) {
  const load = sets.find((set) => set.weightKg !== null)?.weightKg ?? null;
  const reportedEffort = sets.some((set) => set.repsInReserve !== null);

  return (
    <p className="tabular font-mono text-sm text-ink">
      {load !== null && <span className="text-ink-muted">{load} kg · </span>}
      {sets.map((set) => set.reps ?? "-").join(" / ")}
      {!reportedEffort && <span className="ml-2 text-xs text-ink-muted">{noEffortReported}</span>}
    </p>
  );
}

/**
 * One session's prescription beside what was logged against it.
 *
 * The grain is one block per exercise with the performed sequence inline, because the unit is
 * the slot and the sequence is the signal: 11/9/8 and 8/9/11 are different facts, and a total
 * would erase the difference.
 */
function SessionBlock({
  session,
  strings,
  dayLabel,
}: {
  session: SessionComparison;
  strings: ComparisonStrings;
  dayLabel: string;
}) {
  return (
    <Card className="mt-4">
      <CardHeader
        title={dayLabel}
        meta={
          session.performed ? undefined : (
            <span data-testid={`comparison-session-${session.position}-absent`}>
              {strings.notPerformed}
            </span>
          )
        }
      />

      <ul className="flex flex-col gap-4">
        {session.slots.map((slot) => (
          <li key={slot.prescriptionId} data-testid={`comparison-slot-${slot.prescriptionId}`}>
            <div className="flex items-baseline justify-between gap-3">
              <span className="text-sm font-medium text-ink">{slot.exerciseTitle}</span>
              <Pill tone={TONES[slot.outcome] ?? "neutral"}>
                {strings.outcomes[slot.outcome] ?? slot.outcome}
              </Pill>
            </div>

            <p className="tabular font-mono text-xs text-ink-muted">
              {strings.prescribed}: {slot.prescribedSets}×{slot.minReps}-{slot.maxReps} ·{" "}
              {slot.repsInReserve} RIR · {slot.restSeconds}s
            </p>

            {slot.performedSets.length > 0 ? (
              <Sequence sets={slot.performedSets} noEffortReported={strings.noEffortReported} />
            ) : (
              <p className="text-sm text-ink-muted">{strings.notPerformed}</p>
            )}
          </li>
        ))}
      </ul>

      {session.extras.length > 0 && (
        <div className="mt-5 border-t border-line pt-4" data-testid="comparison-extras">
          <h3 className="text-xs font-semibold text-ink">{strings.extrasTitle}</h3>
          <p className="mb-2 text-xs text-ink-muted">{strings.extrasLead}</p>
          <ul className="flex flex-col gap-2">
            {session.extras.map((extra) => (
              <li key={extra.externalTemplateId}>
                <span className="text-sm text-ink">{extra.title ?? extra.externalTemplateId}</span>
                <Sequence sets={extra.sets} noEffortReported={strings.noEffortReported} />
              </li>
            ))}
          </ul>
        </div>
      )}
    </Card>
  );
}

export function ComparisonView({
  comparison,
  strings,
  dayLabels,
}: {
  comparison: Comparison;
  strings: ComparisonStrings;
  dayLabels: string[];
}) {
  return (
    <section className="mt-10" data-testid="comparison">
      <h2 className="text-base font-semibold text-ink">{strings.title}</h2>
      <p className="text-sm text-ink-muted">{strings.lead}</p>
      <p className="mt-1 text-xs text-ink-muted" data-testid="comparison-coverage">
        {strings.coverage}
      </p>

      {comparison.sessions.map((session, index) => (
        <SessionBlock
          key={session.position}
          session={session}
          strings={strings}
          dayLabel={dayLabels[index] ?? session.day ?? String(session.position)}
        />
      ))}

      {comparison.unboundWorkouts.length > 0 && (
        <Card className="mt-4">
          {/* The identifier goes on an element that renders it. Card takes className and
              children only, and TypeScript waves hyphenated JSX attributes through -- so a
              data-testid handed to it would type-check and never reach the DOM. */}
          <div data-testid="comparison-unbound">
            <CardHeader title={strings.unboundTitle} />
            <p className="mb-2 text-xs text-ink-muted">{strings.unboundLead}</p>
            <ul className="flex flex-col gap-1">
              {comparison.unboundWorkouts.map((workout) => (
                <li
                  key={workout.externalWorkoutId}
                  className="tabular font-mono text-xs text-ink-muted"
                >
                  {new Date(workout.startedAt).toISOString().slice(0, 10)} ·{" "}
                  {strings.exerciseCount(workout.exerciseCount)}
                </li>
              ))}
            </ul>
          </div>
        </Card>
      )}
    </section>
  );
}

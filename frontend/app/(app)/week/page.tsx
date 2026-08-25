import Link from "next/link";
import { cookies } from "next/headers";

import { Button } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { Pill } from "@/components/ui/pill";
import { API_URL } from "@/lib/api";
import { secondsToMinutes, splitDuration } from "@/lib/duration";
import { getDictionary, getLocale } from "@/lib/i18n";
import { formatSessionDay } from "@/lib/week";
import { ComparisonView, type Comparison } from "./comparison";
import { markSessionDone, skipSession } from "./actions";
import { GenerateForm } from "./generate-form";
import { LoopControls } from "./loop-controls";
import { SlotActions, type Candidate } from "./slot-actions";

type Muscle = { muscleGroup: string; role: string };

type Prescription = {
  id: string;
  position: number;
  exerciseId: string;
  exerciseTitle: string;
  externalTemplateId: string;
  sets: number;
  minReps: number;
  maxReps: number;
  repsInReserve: number;
  restSeconds: number;
  /** What the slot exists to train, and what it loads on the way. Codes, never sentences. */
  muscles: Muscle[];
  orderClass: string;
  movementPattern: string;
  equipment: string;
  /** `Full` or `Ceiling` — and the two are not distinguishable from `sets` alone. */
  slotKind: string;
};

type Session = {
  id: string;
  position: number;
  /** `Pending`, `Bound`, `Marked` or `Skipped` — and a skip is never a completion. */
  outcome: string;
  /** Null since ADR-027; still set on plans generated before it. */
  day: string | null;
  kind: string;
  /** Computed on read, never stored — see the API's own note on why. */
  estimatedSeconds: number;
  prescriptions: Prescription[];
};

type MuscleVolume = {
  muscleGroup: string;
  direct: number;
  indirect: number;
  /** The target this plan was built with, not today's constant. */
  target: number;
};

type Week = {
  id: string;
  /** The first session still pending, or null once every one has left the queue. */
  nextSessionPosition: number | null;
  /** Null since ADR-027; still set on plans generated before it. */
  weekStartDate: string | null;
  generatedAt: string;
  daysPerWeek: number;
  sessions: Session[];
  volume: MuscleVolume[];
  shortfalls: { muscleGroup: string; fractionalSets: number; target: number }[];
  uncovered: string[];
};

/** Reads whatever the server has: the current week, and whether a profile exists at all. */
async function read<T>(path: string): Promise<T | null> {
  const cookieStore = await cookies();
  const response = await fetch(`${API_URL}${path}`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  return response.ok ? ((await response.json()) as T) : null;
}

export default async function WeekPage() {
  const [dict, locale, week, profile] = await Promise.all([
    getDictionary(),
    getLocale(),
    read<Week>("/training/weeks/current"),
    read<unknown>("/training/profile"),
  ]);

  const strings = dict.week;

  // Two different absences, and telling them apart is the difference between a screen that
  // helps and one that blames. Without a profile there is nothing to generate *from*; with one
  // there is simply nothing generated *yet*.
  if (!profile) {
    return (
      <>
        <PageHeader title={strings.title} lead={strings.lead} />
        <EmptyState title={strings.noProfileTitle} body={strings.noProfileBody} />
        <div className="mt-6">
          {/* Navigation, not an action, so it is a link and looks like one. */}
          <Link
            href="/profile"
            data-testid="week-go-to-profile"
            className="text-sm font-medium text-accent-ink underline underline-offset-4 hover:text-accent"
          >
            {strings.goToProfile}
          </Link>
        </div>
      </>
    );
  }

  if (!week) {
    return (
      <>
        <PageHeader title={strings.title} lead={strings.lead} />
        <EmptyState title={strings.emptyTitle} body={strings.emptyBody} />
        <div className="mt-6">
          <GenerateForm label={strings.generate} working={strings.generating} />
        </div>
      </>
    );
  }

  // One request per slot, in parallel. Fine at a dozen slots and the first thing to fold into
  // the week response if it ever stops being.
  const candidates = new Map<string, Candidate[]>(
    await Promise.all(
      week.sessions
        .flatMap((session) => session.prescriptions)
        .map(async (prescription): Promise<[string, Candidate[]]> => [
          prescription.id,
          (await read<Candidate[]>(
            `/training/weeks/current/prescriptions/${prescription.id}/candidates`,
          )) ?? [],
        ]),
    ),
  );

  // Both only exist once an account is connected. Read after the week, because the comparison
  // is keyed to it.
  const [connection, comparison] = await Promise.all([
    read<{ connected: boolean }>("/hevy/connection"),
    read<Comparison>(`/training/weeks/${week.id}/comparison`),
  ]);

  const generatedAt = new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(week.generatedAt));

  return (
    <>
      <PageHeader title={strings.title} lead={strings.lead} />

      <div className="mb-6 flex flex-wrap items-center gap-4">
        <GenerateForm
          label={strings.regenerate}
          working={strings.generating}
          variant="secondary"
        />
        <p className="text-xs text-ink-muted" data-testid="week-generated-at">
          {strings.generatedAt} {generatedAt}
        </p>
      </div>

      {connection?.connected && (
        <div className="mb-8" data-testid="week-loop">
          <LoopControls
            weekId={week.id}
            strings={{
              push: dict.hevy.push,
              pushing: dict.hevy.pushing,
              sync: dict.hevy.sync,
              syncing: dict.hevy.syncing,
            }}
          />
        </div>
      )}

      <div className="flex flex-col gap-4" data-testid="week-sessions">
        {week.sessions.map((session) => (
          <Card key={session.position}>
            <CardHeader
              title={
                <span className="flex items-center gap-2">
                  {/* The day is resolved to a real date and named by Intl, so the dictionary
                      carries six session kinds instead of seven weekdays per locale. */}
                  {/* A plan is a queue, so a session has a place rather than a date. A week
                      stored before ADR-027 still carries both and still shows the day it was
                      given — rewriting the past to look like the present would be the lie. */}
                  <span data-testid="session-day">
                    {week.weekStartDate && session.day
                      ? formatSessionDay(week.weekStartDate, session.day, locale)
                      : strings.sessionAt(session.position)}
                  </span>
                  <Pill tone="accent">
                    {strings.kinds[session.kind as keyof typeof strings.kinds] ?? session.kind}
                  </Pill>
                  {session.position === week.nextSessionPosition && (
                    <Pill tone="ok">{strings.nextUp}</Pill>
                  )}
                </span>
              }
              // Seconds arrive canonical and become minutes here (root standard 4). Shown as an
              // estimate rather than a duration because two of the terms behind it are
              // engineering constants — and a visible number is what lets them be wrong out loud.
              meta={
                <span className="flex flex-wrap items-baseline gap-x-3">
                  <span data-testid="session-estimate">
                    {strings.estimate} {secondsToMinutes(session.estimatedSeconds)}{" "}
                    {strings.minutesShort}
                  </span>
                  <span data-testid="session-outcome">
                    {strings.outcomes[session.outcome as keyof typeof strings.outcomes] ??
                      session.outcome}
                  </span>
                </span>
              }
            />

            <ul className="flex flex-col divide-y divide-line">
              {session.prescriptions.map((prescription) => (
                <li
                  key={prescription.position}
                  data-testid="prescription"
                  className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-2.5 first:pt-0 last:pb-0"
                >
                  <span className="flex flex-wrap items-baseline gap-x-2 gap-y-1 text-sm text-ink">
                    {prescription.exerciseTitle}
                    {/* Only the ceiling slot is marked. A full slot is the norm and a badge on
                        it would read as a rank rather than as an explanation. */}
                    {prescription.slotKind === "Ceiling" && (
                      <Pill>{strings.extraSlot}</Pill>
                    )}
                  </span>
                  <span className="tabular flex flex-wrap items-baseline gap-x-3 font-mono text-xs text-ink-muted">
                    <span data-testid="prescription-volume">
                      {prescription.sets} &times; {prescription.minReps}&ndash;
                      {prescription.maxReps}
                    </span>
                    <span>
                      {prescription.repsInReserve} {strings.rir}
                    </span>
                    <span data-testid="prescription-rest">
                      {strings.rest} {formatRest(prescription.restSeconds, strings)}
                    </span>
                  </span>
                  {/* Why this exercise is here, in the order the generator decided it: the
                      muscle that was furthest from its target, then what else the slot loads,
                      then how it is done. No claim about any of it being better. */}
                  <p
                    className="w-full text-xs text-ink-muted"
                    data-testid="prescription-explains"
                  >
                    <span>
                      {strings.trains}{" "}
                      <span className="text-ink">
                        {muscleNames(prescription.muscles, "Primary", strings.muscles)}
                      </span>
                    </span>
                    {prescription.muscles.some((muscle) => muscle.role === "Secondary") && (
                      <>
                        {" · "}
                        {strings.alsoLoads}{" "}
                        {muscleNames(prescription.muscles, "Secondary", strings.muscles)}
                      </>
                    )}
                    {" · "}
                    {strings.classes[prescription.orderClass as keyof typeof strings.classes] ??
                      prescription.orderClass}
                    {" · "}
                    {strings.implements[
                      prescription.equipment as keyof typeof strings.implements
                    ] ?? prescription.equipment}
                  </p>

                  <SlotActions
                    prescriptionId={prescription.id}
                    exerciseId={prescription.exerciseId}
                    candidates={candidates.get(prescription.id) ?? []}
                    strings={{
                      swapTo: strings.swapTo,
                      refuse: strings.refuse,
                      noAlternatives: strings.noAlternatives,
                      swapNote: strings.swapNote,
                      classes: strings.classes,
                      implements: strings.implements,
                    }}
                  />
                </li>
              ))}
            </ul>

            {/* Only the session at the head of the queue can be declared: a plan is worked
                through in order, and offering these on every card would invite the reordering
                ADR-032 rejected. Plain forms, so both work with no JavaScript. */}
            {session.position === week.nextSessionPosition && (
              <div className="mt-4 flex flex-wrap items-center gap-2 border-t border-line pt-4">
                <form action={markSessionDone}>
                  <input type="hidden" name="sessionId" value={session.id} />
                  <Button type="submit" size="sm" data-testid="session-done">
                    {strings.markDone}
                  </Button>
                </form>

                <form action={skipSession}>
                  <input type="hidden" name="sessionId" value={session.id} />
                  <Button
                    type="submit"
                    variant="secondary"
                    size="sm"
                    data-testid="session-skip"
                  >
                    {strings.skipSession}
                  </Button>
                </form>
              </div>
            )}
          </Card>
        ))}
      </div>

      {week.sessions.some((session) =>
        session.prescriptions.some((prescription) => prescription.slotKind === "Ceiling"),
      ) && (
        <p className="mt-4 text-xs text-ink-muted" data-testid="week-extra-note">
          <span className="text-ink">{strings.extraSlot}</span> — {strings.extraSlotNote}
        </p>
      )}

      <section className="mt-8" data-testid="week-volume">
        <Card>
          <CardHeader title={strings.volumeTitle} meta={null} />
          <p className="mb-4 text-xs text-ink-muted">{strings.volumeLead}</p>

          {/* Wide content scrolls inside its own box rather than the page. */}
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <caption className="sr-only">{strings.volumeTitle}</caption>
              <thead>
                <tr className="text-left text-xs text-ink-muted">
                  <th scope="col" className="pb-2 font-normal">
                    {strings.volumeTitle}
                  </th>
                  <th scope="col" className="pb-2 text-right font-normal">
                    {strings.directShort}
                  </th>
                  <th scope="col" className="pb-2 text-right font-normal">
                    {strings.indirectShort}
                  </th>
                  <th scope="col" className="pb-2 text-right font-normal">
                    {strings.ofTarget}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {week.volume.map((entry) => (
                  <tr key={entry.muscleGroup} data-testid="volume-row">
                    <th scope="row" className="py-1.5 text-left font-normal text-ink">
                      {strings.muscles[entry.muscleGroup as keyof typeof strings.muscles] ??
                        entry.muscleGroup}
                    </th>
                    <td className="tabular py-1.5 text-right font-mono text-xs text-ink-muted">
                      {entry.direct}
                    </td>
                    <td className="tabular py-1.5 text-right font-mono text-xs text-ink-muted">
                      {entry.indirect}
                    </td>
                    <td className="tabular py-1.5 text-right font-mono text-xs">
                      <span className="text-ink">{entry.direct + entry.indirect}</span>
                      <span className="text-ink-muted">
                        {" "}
                        {strings.ofTarget} {entry.target}
                      </span>
                      {week.shortfalls.some(
                        (shortfall) => shortfall.muscleGroup === entry.muscleGroup,
                      ) && (
                        <span className="text-ink-muted"> · {strings.shortOfFloor}</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {week.uncovered.length > 0 && (
            <div className="mt-5 border-t border-line pt-4" data-testid="week-uncovered">
              <h3 className="text-sm text-ink">{strings.uncoveredTitle}</h3>
              <p className="mt-1 text-xs text-ink-muted">{strings.uncoveredLead}</p>
              <p className="mt-2 text-xs text-ink">
                {week.uncovered
                  .map(
                    (muscle) =>
                      strings.muscles[muscle as keyof typeof strings.muscles] ?? muscle,
                  )
                  .join(", ")}
              </p>
            </div>
          )}
        </Card>
      </section>

      {connection?.connected && comparison && (
        <ComparisonView
          comparison={comparison}
          dayLabels={comparison.sessions.map((session) =>
            week.weekStartDate && session.day
              ? formatSessionDay(week.weekStartDate, session.day, locale)
              : strings.sessionAt(session.position),
          )}
          strings={{
            title: dict.hevy.comparisonTitle,
            lead: dict.hevy.comparisonLead,
            prescribed: dict.hevy.prescribed,
            performed: dict.hevy.performed,
            notPerformed: dict.hevy.notPerformed,
            noEffortReported: dict.hevy.noEffortReported,
            outcomes: {
              InRange: dict.hevy.outcomeInRange,
              AboveRange: dict.hevy.outcomeAboveRange,
              BelowRange: dict.hevy.outcomeBelowRange,
              Mixed: dict.hevy.outcomeMixed,
              NotPerformed: dict.hevy.notPerformed,
            },
            extrasTitle: dict.hevy.extrasTitle,
            extrasLead: dict.hevy.extrasLead,
            unboundTitle: dict.hevy.unboundTitle,
            unboundLead: dict.hevy.unboundLead,
            coverage: dict.hevy.coverage(
              comparison.coverage.boundWorkouts,
              comparison.coverage.importedWorkouts,
            ),
            exerciseCount: dict.hevy.exerciseCount,
          }}
        />
      )}
    </>
  );
}

/**
 * The muscles of one role, translated and joined. Sorted by the API already, so the order a
 * reader sees is the order the catalogue curates rather than whatever the join produced.
 */
function muscleNames(
  muscles: Muscle[],
  role: string,
  names: Record<string, string>,
): string {
  return muscles
    .filter((muscle) => muscle.role === role)
    .map((muscle) => names[muscle.muscleGroup] ?? muscle.muscleGroup)
    .join(", ");
}

/**
 * Rest arrives in seconds, canonical, and becomes "1 min 30 s" here — at the render edge, which
 * is the only place a rendered unit belongs (root standard 4).
 */
function formatRest(
  seconds: number,
  strings: { minutesShort: string; secondsShort: string },
): string {
  const split = splitDuration(seconds);

  if (split.minutes === 0) return `${split.seconds} ${strings.secondsShort}`;
  if (split.seconds === 0) return `${split.minutes} ${strings.minutesShort}`;

  return `${split.minutes} ${strings.minutesShort} ${split.seconds} ${strings.secondsShort}`;
}

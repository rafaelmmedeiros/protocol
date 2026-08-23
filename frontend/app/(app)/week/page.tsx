import Link from "next/link";
import { cookies } from "next/headers";

import { Card, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { Pill } from "@/components/ui/pill";
import { API_URL } from "@/lib/api";
import { secondsToMinutes, splitDuration } from "@/lib/duration";
import { getDictionary, getLocale } from "@/lib/i18n";
import { formatSessionDay } from "@/lib/week";
import { ComparisonView, type Comparison } from "./comparison";
import { GenerateForm } from "./generate-form";
import { LoopControls } from "./loop-controls";
import { SlotActions, type Candidate } from "./slot-actions";

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
};

type Session = {
  position: number;
  day: string;
  kind: string;
  /** Computed on read, never stored — see the API's own note on why. */
  estimatedSeconds: number;
  prescriptions: Prescription[];
};

type Week = {
  id: string;
  weekStartDate: string;
  generatedAt: string;
  daysPerWeek: number;
  sessions: Session[];
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
                  <span data-testid="session-day">
                    {formatSessionDay(week.weekStartDate, session.day, locale)}
                  </span>
                  <Pill tone="accent">
                    {strings.kinds[session.kind as keyof typeof strings.kinds] ?? session.kind}
                  </Pill>
                </span>
              }
              // Seconds arrive canonical and become minutes here (root standard 4). Shown as an
              // estimate rather than a duration because two of the terms behind it are
              // engineering constants — and a visible number is what lets them be wrong out loud.
              meta={
                <span data-testid="session-estimate">
                  {strings.estimate} {secondsToMinutes(session.estimatedSeconds)}{" "}
                  {strings.minutesShort}
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
                  <span className="text-sm text-ink">{prescription.exerciseTitle}</span>
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
                  <SlotActions
                    prescriptionId={prescription.id}
                    exerciseId={prescription.exerciseId}
                    candidates={candidates.get(prescription.id) ?? []}
                    strings={{
                      swapTo: strings.swapTo,
                      refuse: strings.refuse,
                      noAlternatives: strings.noAlternatives,
                    }}
                  />
                </li>
              ))}
            </ul>
          </Card>
        ))}
      </div>

      {connection?.connected && comparison && (
        <ComparisonView
          comparison={comparison}
          dayLabels={comparison.sessions.map((session) =>
            formatSessionDay(week.weekStartDate, session.day, locale),
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
            sets: dict.common.sets,
          }}
        />
      )}
    </>
  );
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

import { Button } from "@/components/ui/button";
import { excludeExercise, substitute } from "./actions";

export type Candidate = {
  exerciseId: string;
  title: string;
  /**
   * Both already travel in the candidates response and both were discarded here until now. A
   * reader choosing between two alternatives is choosing on exactly these.
   */
  equipment: string;
  orderClass: string;
};

export type SlotActionStrings = {
  swapTo: string;
  refuse: string;
  noAlternatives: string;
  swapNote: string;
  classes: Record<string, string>;
  implements: Record<string, string>;
};

/**
 * What can be done to one slot: swap it for something that trains the same thing, or refuse the
 * exercise outright.
 *
 * A Server Component with plain forms, so this works with no JavaScript and needs no client
 * bundle. Each candidate is its own submit button rather than a select plus a button — one less
 * interaction, and every option is visible and keyboard-reachable without opening anything.
 *
 * Every candidate trains the same primary muscle through the same movement pattern, so what
 * separates them is how the movement is done and where the slot sits — which is why both are
 * shown rather than the title alone. Nothing here ranks them: no selection variable tested
 * changes whole-muscle growth once volume is equated (`TD-016`).
 */
export function SlotActions({
  prescriptionId,
  exerciseId,
  candidates,
  strings,
}: {
  prescriptionId: string;
  exerciseId: string;
  candidates: Candidate[];
  strings: SlotActionStrings;
}) {
  return (
    <div className="mt-1.5 flex w-full flex-wrap items-center gap-1.5">
      {candidates.length === 0 ? (
        <span className="text-xs text-ink-muted">{strings.noAlternatives}</span>
      ) : (
        <>
          {candidates.map((candidate) => (
            <form key={candidate.exerciseId} action={substitute}>
              <input type="hidden" name="prescriptionId" value={prescriptionId} />
              <input type="hidden" name="exerciseId" value={candidate.exerciseId} />
              <Button
                type="submit"
                variant="secondary"
                size="sm"
                data-testid={`swap-${prescriptionId}`}
                title={`${strings.swapTo} ${candidate.title} — ${strings.swapNote}`}
              >
                {candidate.title}
                <span className="ml-1.5 text-ink-muted">
                  {strings.implements[candidate.equipment] ?? candidate.equipment}
                  {" · "}
                  {strings.classes[candidate.orderClass] ?? candidate.orderClass}
                </span>
              </Button>
            </form>
          ))}

          {/* Stated once for the row rather than on every button: the replacement's own class
              decides its repetitions and its rest, so a swap moves both (`ADR-012`). */}
          <span className="w-full text-xs text-ink-muted" data-testid={`swap-note-${prescriptionId}`}>
            {strings.swapNote}
          </span>
        </>
      )}

      <form action={excludeExercise}>
        <input type="hidden" name="exerciseId" value={exerciseId} />
        <Button
          type="submit"
          variant="danger"
          size="sm"
          data-testid={`refuse-${prescriptionId}`}
        >
          {strings.refuse}
        </Button>
      </form>
    </div>
  );
}

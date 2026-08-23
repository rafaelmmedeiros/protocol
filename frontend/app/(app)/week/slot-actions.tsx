import { Button } from "@/components/ui/button";
import { excludeExercise, substitute } from "./actions";

export type Candidate = { exerciseId: string; title: string };

export type SlotActionStrings = {
  swapTo: string;
  refuse: string;
  noAlternatives: string;
};

/**
 * What can be done to one slot: swap it for something that trains the same thing, or refuse the
 * exercise outright.
 *
 * A Server Component with plain forms, so this works with no JavaScript and needs no client
 * bundle. Each candidate is its own submit button rather than a select plus a button — one less
 * interaction, and every option is visible and keyboard-reachable without opening anything.
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
        candidates.map((candidate) => (
          <form key={candidate.exerciseId} action={substitute}>
            <input type="hidden" name="prescriptionId" value={prescriptionId} />
            <input type="hidden" name="exerciseId" value={candidate.exerciseId} />
            <Button
              type="submit"
              variant="secondary"
              size="sm"
              data-testid={`swap-${prescriptionId}`}
              title={`${strings.swapTo} ${candidate.title}`}
            >
              {candidate.title}
            </Button>
          </form>
        ))
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

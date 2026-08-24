"use client";

import { useActionState, useState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { eraseEverything, type EraseState } from "./erase-actions";

export type EraseStrings = {
  title: string;
  lead: string;
  /** What the erase will and will not reach. The panel names both rather than only warning. */
  keeps: string;
  start: string;
  areYouSure: string;
  confirm: string;
  cancel: string;
  erasing: string;
};

function ConfirmButton({ label, busy }: { label: string; busy: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" variant="danger" size="sm" disabled={pending} data-testid="erase-confirm">
      {pending ? busy : label}
    </Button>
  );
}

/**
 * Erasing everything of the signed-in user's (ADR-025).
 *
 * Development only -- the page does not render this at all unless the API says the endpoint
 * exists, and the API is the only authority on that.
 *
 * Two steps, and the second one is not decoration. ADR-025 makes this deliberate or nothing: a
 * single button next to "save" is one mis-click from the state it exists to recover from, and the
 * confirmation also travels as a field so the server never acts on an implied intent.
 */
export function ErasePanel({ strings }: { strings: EraseStrings }) {
  const [asking, setAsking] = useState(false);
  const [state, action] = useActionState<EraseState, FormData>(eraseEverything, { outcome: null });

  return (
    <div data-testid="erase-panel" className="flex flex-col gap-3">
      <p className="text-xs text-ink-muted">{strings.lead}</p>
      <p className="text-xs text-ink-muted">{strings.keeps}</p>

      {asking ? (
        <form action={action} className="flex flex-wrap items-center gap-2">
          <input type="hidden" name="confirmed" value="true" />
          <span className="text-sm text-ink" data-testid="erase-asking">
            {strings.areYouSure}
          </span>
          <ConfirmButton label={strings.confirm} busy={strings.erasing} />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setAsking(false)}
            data-testid="erase-cancel"
          >
            {strings.cancel}
          </Button>
        </form>
      ) : (
        <div>
          <Button
            type="button"
            variant="danger"
            size="sm"
            onClick={() => setAsking(true)}
            data-testid="erase-start"
          >
            {strings.start}
          </Button>
        </div>
      )}

      {state.message && (
        <p className="text-sm text-ink" data-testid="erase-result">
          {state.message}
        </p>
      )}
    </div>
  );
}

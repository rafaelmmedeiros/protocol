"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { saveEquipment, type EquipmentState } from "./actions";

export type Item = { value: string; label: string; owned: boolean; group: string };

export type Group = { key: string; label: string; items: Item[] };

export type EquipmentStrings = {
  itemsLabel: string;
  itemsHint: string;
  groups: Record<string, string>;
  save: string;
  saving: string;
  saved: string;
};

/**
 * Strings arrive translated as props, so no dictionary reaches the browser bundle. A checkbox
 * per item and nothing else: what a movement needs is the catalogue's business, not the
 * reader's.
 */
export function EquipmentForm({ groups, strings }: { groups: Group[]; strings: EquipmentStrings }) {
  const [state, action] = useActionState<EquipmentState, FormData>(saveEquipment, { saved: false });

  return (
    <form action={action} className="flex flex-col gap-6">
      <fieldset className="flex flex-col gap-5">
        <legend className="text-sm font-medium text-ink">{strings.itemsLabel}</legend>
        <p className="text-xs text-ink-muted">{strings.itemsHint}</p>

        {/* Sections rather than one list. Thirty checkboxes in a column is the wall the M2
            complaint was about, and the fix is grouping, not a coarser vocabulary (ADR-022). */}
        {groups.map((group) => (
          <div key={group.key} className="flex flex-col gap-2" data-testid={`equipment-group-${group.key}`}>
            <h3 className="text-xs font-semibold tracking-wide text-ink-muted uppercase">
              {group.label}
            </h3>
            <div className="grid gap-2 sm:grid-cols-2">
              {group.items.map((item) => (
                <Checkbox
                  key={item.value}
                  name="item"
                  value={item.value}
                  label={item.label}
                  defaultChecked={item.owned}
                  data-testid={`equipment-${item.value}`}
                />
              ))}
            </div>
          </div>
        ))}
      </fieldset>

      <Outcome saved={state.saved} savedLabel={strings.saved} error={state.error}>
        <SubmitButton save={strings.save} saving={strings.saving} />
      </Outcome>
    </form>
  );
}

/**
 * The result of the last save, hidden while the next is in flight — the same reason the profile
 * form does it: a "saved" beside a pending submit claims something untrue.
 */
function Outcome({
  saved,
  savedLabel,
  error,
  children,
}: {
  saved: boolean;
  savedLabel: string;
  error?: string;
  children: React.ReactNode;
}) {
  const { pending } = useFormStatus();

  return (
    <div className="flex items-center gap-3">
      {children}
      {!pending && saved && (
        <p data-testid="equipment-saved" role="status" className="text-sm text-ok">
          {savedLabel}
        </p>
      )}
      {!pending && error && (
        <p data-testid="equipment-error" role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

function SubmitButton({ save, saving }: { save: string; saving: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" data-testid="equipment-submit" disabled={pending}>
      {pending ? saving : save}
    </Button>
  );
}

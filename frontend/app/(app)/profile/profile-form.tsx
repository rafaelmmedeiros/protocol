"use client";

import { useActionState, useState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Select } from "@/components/ui/select";
import { saveProfile, type ProfileState } from "./actions";

export type GoalChoice = {
  value: string;
  label: string;
  /** Collected by the schema, not programmed yet (ADR-004). */
  available: boolean;
};

export type ProfileStrings = {
  goalLabel: string;
  goalHint: string;
  unavailable: string;
  daysLabel: string;
  daysHint: string;
  durationLabel: string;
  durationHint: string;
  splitLabel: string;
  splitHint: string;
  splitDefault: string;
  splits: Record<string, string>;
  save: string;
  saving: string;
  saved: string;
};

/**
 * The strings are translated on the server and arrive as props, so no dictionary reaches the
 * browser bundle. The error sentence arrives already resolved for the same reason: this
 * component never sees a code.
 */
export function ProfileForm({
  goals,
  strings,
  currentGoal,
  currentDays,
  currentMinutes,
  currentSplit,
  splitOptions,
  bounds,
}: {
  goals: GoalChoice[];
  strings: ProfileStrings;
  currentGoal: string;
  currentDays: number;
  currentMinutes: number;
  /** What was chosen, or empty for the frequency's default (ADR-030). */
  currentSplit: string;
  /** Every frequency's templates (TD-023), served by the API and never listed in this tier. */
  splitOptions: { daysPerWeek: number; templates: string[] }[];
  bounds: { minDays: number; maxDays: number; minMinutes: number; maxMinutes: number };
}) {
  const [state, action] = useActionState<ProfileState, FormData>(saveProfile, { saved: false });
  const message = state.error;

  // Which splits exist depends on the frequency, and the frequency is edited right here — so
  // the list follows the field rather than the saved value. The table comes from the API, so
  // filtering it is not a second copy of TD-023.
  const [days, setDays] = useState(currentDays);
  const admittedSplits =
    splitOptions.find((option) => option.daysPerWeek === days)?.templates ?? [];

  return (
    <form action={action} className="flex max-w-md flex-col gap-6">
      <Select
        id="goal"
        name="goal"
        data-testid="profile-goal"
        label={strings.goalLabel}
        hint={strings.goalHint}
        defaultValue={currentGoal}
        options={goals.map((goal) => ({
          value: goal.value,
          label: goal.available ? goal.label : `${goal.label} (${strings.unavailable})`,
          disabled: !goal.available,
        }))}
      />

      <Field
        id="daysPerWeek"
        name="daysPerWeek"
        type="number"
        inputMode="numeric"
        data-testid="profile-days"
        label={strings.daysLabel}
        hint={strings.daysHint}
        defaultValue={currentDays}
        onChange={(event) => setDays(Number(event.target.value))}
        min={bounds.minDays}
        max={bounds.maxDays}
        required
      />

      <Select
        id="split"
        name="split"
        data-testid="profile-split"
        label={strings.splitLabel}
        hint={strings.splitHint}
        defaultValue={currentSplit}
        // Keyed by the frequency so the browser re-reads defaultValue when the list changes;
        // without it a choice made for five days stays selected under four and is then refused.
        key={days}
        options={[
          // The empty value is a real answer and is listed first: it means "whatever this
          // frequency maps to", which is what a user who never chose is on.
          { value: "", label: strings.splitDefault },
          ...admittedSplits.map((id) => ({
            value: id,
            // No option is marked as recommended or as the default. Split organisation has no
            // detectable effect on growth once volume is equated, and a badge here is exactly
            // where that claim would come back (TD-023).
            label: strings.splits[id] ?? id,
          })),
        ]}
      />

      <Field
        id="durationMinutes"
        name="durationMinutes"
        type="number"
        inputMode="numeric"
        data-testid="profile-duration"
        label={strings.durationLabel}
        hint={strings.durationHint}
        defaultValue={currentMinutes}
        min={bounds.minMinutes}
        max={bounds.maxMinutes}
        required
      />

      <Outcome saved={state.saved} savedLabel={strings.saved} error={message}>
        <SubmitButton save={strings.save} saving={strings.saving} />
      </Outcome>
    </form>
  );
}

/**
 * The result of the *last* save, hidden while the next one is in flight. A "saved" message
 * sitting next to a pending submit claims something that is not true yet, and it is also what
 * makes a test unable to tell one save from the next.
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
        <p data-testid="profile-saved" role="status" className="text-sm text-ok">
          {savedLabel}
        </p>
      )}
      {!pending && error && (
        <p data-testid="profile-error" role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

function SubmitButton({ save, saving }: { save: string; saving: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" data-testid="profile-submit" disabled={pending}>
      {pending ? saving : save}
    </Button>
  );
}

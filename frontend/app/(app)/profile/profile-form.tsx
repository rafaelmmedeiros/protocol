"use client";

import { useActionState } from "react";
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
  bounds,
}: {
  goals: GoalChoice[];
  strings: ProfileStrings;
  currentGoal: string;
  currentDays: number;
  currentMinutes: number;
  bounds: { minDays: number; maxDays: number; minMinutes: number; maxMinutes: number };
}) {
  const [state, action] = useActionState<ProfileState, FormData>(saveProfile, { saved: false });
  const message = state.error;

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
        min={bounds.minDays}
        max={bounds.maxDays}
        required
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

      <div className="flex items-center gap-3">
        <SubmitButton save={strings.save} saving={strings.saving} />
        {state.saved && (
          <p data-testid="profile-saved" role="status" className="text-sm text-ok">
            {strings.saved}
          </p>
        )}
        {message && (
          <p data-testid="profile-error" role="alert" className="text-sm text-danger">
            {message}
          </p>
        )}
      </div>
    </form>
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

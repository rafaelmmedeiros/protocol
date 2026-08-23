"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { pushWeek, syncHevy, type LoopState } from "./hevy-actions";

export type LoopStrings = {
  push: string;
  pushing: string;
  sync: string;
  syncing: string;
};

/**
 * The two deliberate actions of the loop.
 *
 * Both are buttons the user presses, never something that happens on a render: one writes into
 * a third party's account, and the other reads it over many sequential calls. Each carries its
 * own outcome so a failed sync cannot look like a failed push.
 */
export function LoopControls({ weekId, strings }: { weekId: string; strings: LoopStrings }) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
      <PushForm weekId={weekId} strings={strings} />
      <SyncForm strings={strings} />
    </div>
  );
}

function PushForm({ weekId, strings }: { weekId: string; strings: LoopStrings }) {
  const [state, action] = useActionState<LoopState, FormData>(pushWeek, {});

  return (
    <form action={action} className="flex flex-col gap-2">
      <input type="hidden" name="weekId" value={weekId} />
      <Outcome state={state} testId="push">
        <SubmitButton idle={strings.push} busy={strings.pushing} testId="week-push" />
      </Outcome>
    </form>
  );
}

function SyncForm({ strings }: { strings: LoopStrings }) {
  const [state, action] = useActionState<LoopState, FormData>(syncHevy, {});

  return (
    <form action={action} className="flex flex-col gap-2">
      <Outcome state={state} testId="sync">
        <SubmitButton idle={strings.sync} busy={strings.syncing} testId="week-sync" />
      </Outcome>
    </form>
  );
}

/** Hidden while the next submit is in flight — a stale result beside a pending action lies. */
function Outcome({
  state,
  testId,
  children,
}: {
  state: LoopState;
  testId: string;
  children: React.ReactNode;
}) {
  const { pending } = useFormStatus();

  return (
    <div className="flex flex-col gap-1">
      {children}
      {!pending && state.message && (
        <p role="status" data-testid={`${testId}-result`} className="text-xs text-ok">
          {state.message}
        </p>
      )}
      {!pending && state.error && (
        <p role="alert" data-testid={`${testId}-error`} className="text-xs text-danger">
          {state.error}
        </p>
      )}
    </div>
  );
}

function SubmitButton({ idle, busy, testId }: { idle: string; busy: string; testId: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" disabled={pending} data-testid={testId}>
      {pending ? busy : idle}
    </Button>
  );
}

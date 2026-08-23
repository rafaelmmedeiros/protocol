"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Pill } from "@/components/ui/pill";
import { connectHevy, type HevyConnectionState } from "./hevy-actions";

export type HevyStrings = {
  apiKeyLabel: string;
  apiKeyHelp: string;
  connect: string;
  connecting: string;
  connected: string;
  notConnected: string;
  connectedSince: string | null;
  keyNeverShown: string;
};

/**
 * Connecting a Hevy account.
 *
 * There is no field showing the current key, and there is no masked version of it either: the
 * API has no endpoint that returns one (ADR-014), so this component could not display it if it
 * wanted to. What it shows is whether an account is connected, which is the only fact there is.
 */
export function HevyConnection({
  connected,
  strings,
}: {
  connected: boolean;
  strings: HevyStrings;
}) {
  const [state, action] = useActionState<HevyConnectionState, FormData>(connectHevy, {
    connected,
  });

  const isConnected = connected || state.connected;

  return (
    <form action={action} className="flex flex-col gap-4">
      <div className="flex items-center gap-2" data-testid="hevy-status">
        <Pill tone={isConnected ? "ok" : "neutral"}>
          {isConnected ? strings.connected : strings.notConnected}
        </Pill>
        {isConnected && strings.connectedSince ? (
          <span className="text-xs text-ink-muted">{strings.connectedSince}</span>
        ) : null}
      </div>

      <Field
        label={strings.apiKeyLabel}
        hint={strings.apiKeyHelp}
        name="apiKey"
        // A credential, so the browser treats it as one: never autofilled from a previous value,
        // never rendered in the clear, and never repopulated after the round trip.
        type="password"
        autoComplete="off"
        required
        data-testid="hevy-api-key"
      />

      <Outcome
        error={state.error}
        saved={state.connected}
        savedLabel={strings.keyNeverShown}
      >
        <SubmitButton connect={strings.connect} connecting={strings.connecting} />
      </Outcome>
    </form>
  );
}

/**
 * The result of the last attempt, hidden while the next is in flight — a "saved" beside a
 * pending submit claims something untrue.
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
        <p role="status" data-testid="hevy-saved" className="text-sm text-ok">
          {savedLabel}
        </p>
      )}
      {!pending && error && (
        <p role="alert" data-testid="hevy-error" className="text-sm text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

function SubmitButton({ connect, connecting }: { connect: string; connecting: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" disabled={pending} data-testid="hevy-connect">
      {pending ? connecting : connect}
    </Button>
  );
}

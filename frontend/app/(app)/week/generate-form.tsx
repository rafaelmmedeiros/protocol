"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { generateWeek, type GenerateState } from "./actions";

export function GenerateForm({
  label,
  working,
  variant = "primary",
}: {
  label: string;
  working: string;
  variant?: "primary" | "secondary";
}) {
  const [state, action] = useActionState<GenerateState, FormData>(generateWeek, {});

  return (
    <form action={action} className="flex items-center gap-3">
      <SubmitButton label={label} working={working} variant={variant} />
      {state.error && (
        <p data-testid="week-error" role="alert" className="text-sm text-danger">
          {state.error}
        </p>
      )}
    </form>
  );
}

function SubmitButton({
  label,
  working,
  variant,
}: {
  label: string;
  working: string;
  variant: "primary" | "secondary";
}) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" variant={variant} data-testid="week-generate" disabled={pending}>
      {pending ? working : label}
    </Button>
  );
}

"use client";

import type { InputHTMLAttributes } from "react";
import { useId } from "react";

import { cn } from "@/lib/cn";

type Props = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
};

/**
 * A real `<label>` bound to the input, always. Placeholder text is not a label: it disappears
 * the moment someone types, and screen readers do not treat it as a name.
 */
export function Field({ label, hint, className, id, ...props }: Props) {
  const generated = useId();
  const inputId = id ?? generated;
  const hintId = hint ? `${inputId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={inputId} className="text-sm font-medium text-ink">
        {label}
      </label>
      <input
        {...props}
        id={inputId}
        aria-describedby={hintId}
        className={cn(
          "rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink",
          "placeholder:text-ink-muted/70 hover:border-line-strong",
          "disabled:opacity-50",
          className,
        )}
      />
      {hint && (
        <p id={hintId} className="text-xs text-ink-muted">
          {hint}
        </p>
      )}
    </div>
  );
}

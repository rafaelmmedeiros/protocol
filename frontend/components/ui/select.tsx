"use client";

import type { SelectHTMLAttributes } from "react";
import { useId } from "react";

import { cn } from "@/lib/cn";

export type SelectOption = {
  value: string;
  label: string;
  /** A choice the product shows but does not offer yet. */
  disabled?: boolean;
};

type Props = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  hint?: string;
  options: SelectOption[];
};

/**
 * A real `<select>` with a real `<label>`, matching `Field` so a form does not look assembled
 * from two different kits. Native rather than a custom listbox: the platform control is
 * keyboard-reachable, screen-reader correct and usable on a phone without any of it being
 * rebuilt — and none of that survives being reimplemented in divs.
 *
 * A disabled option is deliberately still rendered. A choice the product will support but does
 * not yet is information; hiding it makes the roadmap invisible and the field look arbitrary.
 */
export function Select({ label, hint, options, className, id, ...props }: Props) {
  const generated = useId();
  const selectId = id ?? generated;
  const hintId = hint ? `${selectId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={selectId} className="text-sm font-medium text-ink">
        {label}
      </label>
      <select
        {...props}
        id={selectId}
        aria-describedby={hintId}
        className={cn(
          "rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink",
          "hover:border-line-strong disabled:opacity-50",
          className,
        )}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>
      {hint && (
        <p id={hintId} className="text-xs text-ink-muted">
          {hint}
        </p>
      )}
    </div>
  );
}

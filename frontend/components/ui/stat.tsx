import type { ReactNode } from "react";

/**
 * One number, said once. The value is tabular so a column of these lines up, and an absent
 * value is an em dash rather than a zero -- "no data" and "zero sets" are different facts.
 */
export function Stat({
  label,
  value,
  unit,
  caption,
}: {
  label: string;
  value?: ReactNode;
  unit?: string;
  caption?: string;
}) {
  return (
    <div className="rounded-lg border border-line bg-surface p-5">
      <p className="eyebrow">{label}</p>
      <p className="tabular mt-2 text-3xl leading-none font-semibold text-ink">
        {value ?? <span className="text-ink-muted">&mdash;</span>}
        {unit && value != null && (
          <span className="ml-1.5 text-sm font-medium text-ink-muted">{unit}</span>
        )}
      </p>
      {caption && <p className="mt-2 text-xs text-ink-muted">{caption}</p>}
    </div>
  );
}

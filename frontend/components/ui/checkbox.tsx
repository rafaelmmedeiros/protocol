import type { InputHTMLAttributes } from "react";

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
};

/**
 * A checkbox inside its own `<label>`, so the whole row is the hit target and no `htmlFor` can
 * drift from an id. Native rather than a styled div: the platform control is keyboard-reachable
 * and announced correctly, and none of that survives being rebuilt.
 *
 * A Server Component — it holds no state of its own, so it costs nothing in the browser bundle.
 */
export function Checkbox({ label, className, ...props }: Props) {
  return (
    <label className="flex items-center gap-2.5 rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink hover:border-line-strong">
      <input
        {...props}
        type="checkbox"
        className={className ?? "size-4 accent-[var(--color-accent-fill)]"}
      />
      {label}
    </label>
  );
}

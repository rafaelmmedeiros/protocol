import type { ReactNode } from "react";

import { cn } from "@/lib/cn";

/**
 * State carries a dot as well as a colour. Colour alone fails for a colourblind reader, in
 * greyscale print, and in a screenshot pasted into a chat -- the dot's position and the label
 * survive all three.
 */
export type Tone = "neutral" | "accent" | "ok" | "warn" | "bad";

const TONES: Record<Tone, { wrap: string; dot: string }> = {
  neutral: { wrap: "border-line bg-surface-muted text-ink-muted", dot: "bg-ink-muted" },
  accent: { wrap: "border-accent/30 bg-accent-soft text-accent-ink", dot: "bg-accent" },
  ok: { wrap: "border-ok/30 bg-ok-soft text-ok", dot: "bg-ok" },
  warn: { wrap: "border-warn/30 bg-warn-soft text-warn", dot: "bg-warn" },
  bad: { wrap: "border-bad/30 bg-bad-soft text-bad", dot: "bg-bad" },
};

export function Pill({ tone = "neutral", children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium",
        TONES[tone].wrap,
      )}
    >
      <span aria-hidden className={cn("size-1.5 rounded-full", TONES[tone].dot)} />
      {children}
    </span>
  );
}

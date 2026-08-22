import type { ReactNode } from "react";

import { cn } from "@/lib/cn";

export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <section
      className={cn(
        "rounded-lg border border-line bg-surface p-5 shadow-[var(--shadow-card)]",
        className,
      )}
    >
      {children}
    </section>
  );
}

export function CardHeader({ title, meta }: { title: ReactNode; meta?: ReactNode }) {
  return (
    <header className="mb-3 flex items-baseline justify-between gap-3">
      <h2 className="text-sm font-semibold text-ink">{title}</h2>
      {meta && <span className="tabular font-mono text-xs text-ink-muted">{meta}</span>}
    </header>
  );
}

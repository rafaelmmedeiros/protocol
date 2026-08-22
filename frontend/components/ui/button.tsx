import type { ButtonHTMLAttributes } from "react";

import { cn } from "@/lib/cn";

/**
 * `danger` is deliberately not a filled button. Ember and red are both warm, and a filled
 * ember button beside a filled red one is the single pairing in this palette that reads
 * ambiguously -- so a destructive action is an outline with red text instead.
 */
export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

const VARIANTS: Record<ButtonVariant, string> = {
  primary: "bg-accent-fill text-on-accent hover:bg-accent-fill-hover",
  secondary: "border border-line-strong bg-surface text-ink hover:bg-surface-muted",
  ghost: "text-ink-muted hover:bg-surface-muted hover:text-ink",
  danger: "border border-bad/40 text-bad hover:bg-bad-soft",
};

const SIZES = {
  sm: "px-3 py-1.5 text-xs",
  md: "px-4 py-2 text-sm",
} as const;

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: keyof typeof SIZES;
};

export function Button({ variant = "primary", size = "md", className, ...props }: Props) {
  return (
    <button
      {...props}
      className={cn(
        "inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors",
        "disabled:pointer-events-none disabled:opacity-50",
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
    />
  );
}

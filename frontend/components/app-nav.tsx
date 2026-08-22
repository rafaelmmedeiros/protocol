"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { cn } from "@/lib/cn";

export type NavItem = {
  href: string;
  label: string;
  /** Stable across locales, so Playwright never selects on translated text. */
  testId: string;
};

/**
 * Client-side only because it needs the current path to mark the active tab. The labels are
 * translated on the server and arrive as props, so no dictionary reaches the browser bundle.
 */
export function AppNav({ items, label }: { items: NavItem[]; label: string }) {
  const pathname = usePathname();

  return (
    <nav aria-label={label} className="flex items-center gap-1">
      {items.map((item) => {
        const active = pathname === item.href || pathname.startsWith(`${item.href}/`);

        return (
          <Link
            key={item.href}
            href={item.href}
            data-testid={item.testId}
            aria-current={active ? "page" : undefined}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm transition-colors",
              active
                ? "bg-accent-soft font-medium text-accent-ink"
                : "text-ink-muted hover:bg-surface-muted hover:text-ink",
            )}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}

"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";

export type UserMenuStrings = {
  menu: string;
  signedInAs: string;
  settings: string;
  signOut: string;
  signingOut: string;
};

/**
 * A hand-rolled menu rather than a component library: this is the only popover in the app so
 * far, and it is thirty lines. The parts that are easy to get wrong are all here -- Escape
 * closes it, a click outside closes it, focus returns to the trigger, and `aria-expanded`
 * tells a screen reader which state it is in.
 */
export function UserMenu({ email, strings }: { email: string; strings: UserMenuStrings }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const container = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;

    function onPointerDown(event: MouseEvent) {
      if (!container.current?.contains(event.target as Node)) setOpen(false);
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      setOpen(false);
      trigger.current?.focus();
    }

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  async function signOut() {
    setSigningOut(true);
    await fetch("/api/auth/logout", { method: "POST" });
    router.replace("/login");
    router.refresh();
  }

  return (
    <div ref={container} className="relative">
      <button
        ref={trigger}
        type="button"
        data-testid="user-menu"
        aria-label={strings.menu}
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((wasOpen) => !wasOpen)}
        className="flex size-8 items-center justify-center rounded-full bg-accent-fill text-xs font-semibold text-on-accent transition-colors hover:bg-accent-fill-hover"
      >
        {initials(email)}
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-20 mt-2 w-60 overflow-hidden rounded-lg border border-line bg-surface shadow-[var(--shadow-card)]"
        >
          <div className="border-b border-line px-4 py-3">
            <p className="text-xs text-ink-muted">{strings.signedInAs}</p>
            <p data-testid="menu-email" className="truncate text-sm font-medium text-ink">
              {email}
            </p>
          </div>

          <Link
            role="menuitem"
            href="/settings"
            data-testid="menu-settings"
            onClick={() => setOpen(false)}
            className="block px-4 py-2.5 text-sm text-ink transition-colors hover:bg-surface-muted"
          >
            {strings.settings}
          </Link>

          <button
            role="menuitem"
            type="button"
            data-testid="logout"
            onClick={signOut}
            disabled={signingOut}
            className="block w-full px-4 py-2.5 text-left text-sm text-ink transition-colors hover:bg-surface-muted disabled:opacity-50"
          >
            {signingOut ? strings.signingOut : strings.signOut}
          </button>
        </div>
      )}
    </div>
  );
}

/** The local part of the email is the only name this system has so far. */
function initials(email: string): string {
  const local = email.split("@")[0] ?? "";
  const parts = local.split(/[._-]+/).filter(Boolean);
  const letters = parts.length > 1 ? `${parts[0][0]}${parts[1][0]}` : local.slice(0, 2);
  return letters.toUpperCase() || "?";
}

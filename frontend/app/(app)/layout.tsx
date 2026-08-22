import { redirect } from "next/navigation";

import { AppNav, type NavItem } from "@/components/app-nav";
import { UserMenu } from "@/components/user-menu";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

/**
 * The authenticated shell. A route group leaves the URLs alone -- `/dashboard` is still
 * `/dashboard` -- while giving every page inside it the same chrome and the same guard, so a
 * new page cannot forget to check for a session.
 */
export default async function AppLayout({ children }: LayoutProps<"/">) {
  const [user, dict] = await Promise.all([getCurrentUser(), getDictionary()]);
  if (!user) redirect("/login");

  const items: NavItem[] = [
    { href: "/dashboard", label: dict.nav.dashboard, testId: "nav-dashboard" },
    { href: "/workouts", label: dict.nav.workouts, testId: "nav-workouts" },
    { href: "/equipment", label: dict.nav.equipment, testId: "nav-equipment" },
    { href: "/template", label: dict.nav.template, testId: "nav-template" },
  ];

  return (
    <>
      <a
        href="#content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-3 focus:left-3 focus:z-30 focus:rounded-md focus:bg-surface focus:px-3 focus:py-2 focus:text-sm focus:text-ink focus:shadow-[var(--shadow-card)]"
      >
        {dict.nav.skipToContent}
      </a>

      <header className="sticky top-0 z-10 border-b border-line bg-surface/85 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-6">
          <span className="text-base font-semibold text-ink">{dict.app.name}</span>
          <AppNav items={items} label={dict.nav.primary} />
          <div className="ml-auto">
            <UserMenu email={user.email} strings={dict.user} />
          </div>
        </div>
      </header>

      <main id="content" className="mx-auto w-full max-w-5xl grow px-6 py-10">
        {children}
      </main>
    </>
  );
}

import { Card, CardHeader } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { Stat } from "@/components/ui/stat";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

/**
 * There is no training data yet, so every figure is honestly absent rather than faked. The
 * tiles exist because the shape of this screen is a decision worth making now: three headline
 * numbers, then the detail.
 */
export default async function DashboardPage() {
  const [user, dict] = await Promise.all([getCurrentUser(), getDictionary()]);
  if (!user) return null; // The layout already redirected; this only satisfies the type.

  return (
    <>
      <PageHeader title={dict.dashboard.title} lead={dict.dashboard.lead} />

      <div className="grid gap-4 sm:grid-cols-3">
        <Stat label={dict.dashboard.weeklySets} caption={dict.common.awaitingImport} />
        <Stat label={dict.dashboard.weeklyVolume} caption={dict.common.awaitingImport} />
        <Stat label={dict.dashboard.lastSession} caption={dict.common.awaitingImport} />
      </div>

      <Card className="mt-4">
        <CardHeader title={dict.dashboard.session} />
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-xs text-ink-muted">{dict.dashboard.email}</dt>
            <dd data-testid="user-email" className="font-medium text-ink">
              {user.email}
            </dd>
          </div>
          <div>
            <dt className="text-xs text-ink-muted">{dict.dashboard.userId}</dt>
            <dd className="tabular font-mono text-xs break-all text-ink-muted">{user.id}</dd>
          </div>
        </dl>
      </Card>
    </>
  );
}

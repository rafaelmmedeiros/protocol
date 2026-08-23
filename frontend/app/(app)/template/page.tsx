import { Button } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Field } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";
import { Pill } from "@/components/ui/pill";
import { Select } from "@/components/ui/select";
import { Stat } from "@/components/ui/stat";
import { getDictionary } from "@/lib/i18n";

/**
 * The living style guide. It renders the real components rather than pictures of them, so it
 * cannot drift: a component that changes changes here in the same commit, and anything on this
 * page that no longer matches the product is a bug in one of the two.
 *
 * The swatch class strings are written out in full on purpose -- Tailwind reads source text,
 * so a class assembled from a variable would never be generated.
 */
const SURFACE_TOKENS = [
  { token: "ground", swatch: "bg-ground", role: "The page behind everything" },
  { token: "surface", swatch: "bg-surface", role: "What lifts off the ground: card, header, field" },
  { token: "surface-muted", swatch: "bg-surface-muted", role: "Hover fill, quiet row" },
  { token: "line", swatch: "bg-line", role: "Border and divider" },
  { token: "line-strong", swatch: "bg-line-strong", role: "Border that has to be seen" },
  { token: "ink", swatch: "bg-ink", role: "Text that carries meaning" },
  { token: "ink-muted", swatch: "bg-ink-muted", role: "Label, caption, unit" },
];

const ACCENT_TOKENS = [
  { token: "accent", swatch: "bg-accent", role: "The mark: active tab, indicator, chart bar" },
  { token: "accent-ink", swatch: "bg-accent-ink", role: "Accent as text, contrast-safe" },
  {
    token: "accent-fill",
    swatch: "bg-accent-fill",
    role: "Behind on-accent text: the primary button",
  },
  { token: "accent-soft", swatch: "bg-accent-soft", role: "Tinted background, active nav item" },
  { token: "on-accent", swatch: "bg-on-accent", role: "Whatever sits on accent-fill" },
];

const RESERVED_TOKENS = [
  { token: "ok", swatch: "bg-ok", role: "Progress. A set added, a lift up" },
  { token: "warn", swatch: "bg-warn", role: "Attention. Deload, accumulated fatigue" },
  { token: "bad", swatch: "bg-bad", role: "Regression, and form errors" },
];

const STACK = [
  {
    choice: "Next.js 16 / React 19",
    why: "App Router; the browser only ever talks to this origin",
  },
  { choice: "Tailwind CSS v4", why: "Tokens declared in @theme; no config file" },
  {
    choice: "Own components",
    why: "Seven of them, on the tokens. A library would decide the palette for us",
  },
  {
    choice: "Own dictionary",
    why: "Two locales as typed modules; the compiler catches a missing key",
  },
  {
    choice: "Archivo (SIL OFL)",
    why: "Self-hosted from public/, so the build never touches the network",
  },
  { choice: "Vitest / Playwright", why: "Unit over lib/, end to end in Docker on its own stack" },
];

export default async function TemplatePage() {
  const dict = await getDictionary();

  return (
    <>
      <PageHeader title={dict.template.title} lead={dict.template.lead} />

      <div className="flex flex-col gap-12">
        <Section title={dict.template.colour} lead={dict.template.colourLead}>
          <TokenTable
            rows={SURFACE_TOKENS}
            tokenColumn={dict.template.tokenColumn}
            roleColumn={dict.template.roleColumn}
          />
          <TokenTable
            rows={ACCENT_TOKENS}
            tokenColumn={dict.template.tokenColumn}
            roleColumn={dict.template.roleColumn}
          />
        </Section>

        <Section title={dict.template.reserved} lead={dict.template.reservedLead}>
          <TokenTable
            rows={RESERVED_TOKENS}
            tokenColumn={dict.template.tokenColumn}
            roleColumn={dict.template.roleColumn}
          />
        </Section>

        <Section title={dict.template.typography} lead={dict.template.typographyLead}>
          <Card>
            <div className="flex flex-col gap-4">
              <p className="text-3xl font-semibold text-ink">Aa &mdash; 3xl, a headline number</p>
              <p className="text-2xl font-semibold text-ink">Aa &mdash; 2xl, page title</p>
              <p className="text-lg font-semibold text-ink">Aa &mdash; lg, section heading</p>
              <p className="text-sm font-semibold text-ink">Aa &mdash; sm semibold, card title</p>
              <p className="text-sm text-ink">Aa &mdash; sm, body</p>
              <p className="text-xs text-ink-muted">Aa &mdash; xs, caption</p>
              <p className="eyebrow">Aa &mdash; eyebrow, a label above a number</p>
              <p className="font-mono text-xs text-ink-muted">Aa &mdash; xs mono, identifiers</p>

              {/* The two columns must hold *different* digits, or nothing can misalign and
                  the demonstration proves nothing. Measured: Archivo sets 1111 at 83px and
                  8888 at 92px; tabular figures put both at 91px. */}
              <div className="mt-2 grid max-w-md grid-cols-2 gap-6 border-t border-line pt-4">
                <div>
                  <p className="eyebrow mb-1">Tabular</p>
                  <p className="tabular text-right text-sm text-ink">118,5 kg</p>
                  <p className="tabular text-right text-sm text-ink">91,0 kg</p>
                  <p className="tabular text-right text-sm text-ink">8,5 kg</p>
                </div>
                <div>
                  <p className="eyebrow mb-1">Proportional</p>
                  <p className="text-right text-sm text-ink">118,5 kg</p>
                  <p className="text-right text-sm text-ink">91,0 kg</p>
                  <p className="text-right text-sm text-ink">8,5 kg</p>
                </div>
              </div>
              <p className="text-xs text-ink-muted">
                Same numbers, same alignment. Only the left column has its commas and units in
                a straight line, which is what a table of weights needs.
              </p>
            </div>
          </Card>
        </Section>

        <Section title={dict.template.buttons} lead={dict.template.buttonsLead}>
          <Card>
            <div className="flex flex-wrap items-center gap-3">
              <Button>Primary</Button>
              <Button variant="secondary">Secondary</Button>
              <Button variant="ghost">Ghost</Button>
              <Button variant="danger">Danger</Button>
              <Button disabled>Disabled</Button>
              <Button size="sm" variant="secondary">
                Small
              </Button>
            </div>
          </Card>
        </Section>

        <Section title={dict.template.fields} lead={dict.template.fieldsLead}>
          <Card>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={dict.dashboard.email} type="email" defaultValue="you@protocol.test" />
              <Field
                label="Body weight"
                type="text"
                defaultValue="82.4"
                hint="Stored in kilograms; the unit shown follows the locale."
              />
              <Select
                label={dict.profile.goalLabel}
                hint={dict.profile.goalHint}
                defaultValue="Hypertrophy"
                options={[
                  { value: "Hypertrophy", label: dict.profile.goalHypertrophy },
                  {
                    value: "Strength",
                    label: `${dict.profile.goalStrength} (${dict.profile.unavailable})`,
                    disabled: true,
                  },
                ]}
              />
            </div>
          </Card>
        </Section>

        <Section title={dict.template.feedback} lead={dict.template.feedbackLead}>
          <Card>
            <CardHeader title="Weekly volume / Chest" meta="12 wk" />
            <div className="flex flex-wrap gap-2">
              <Pill tone="ok">Progressing</Pill>
              <Pill tone="warn">Deload</Pill>
              <Pill tone="bad">Stalled</Pill>
              <Pill tone="accent">Proposed</Pill>
              <Pill>{dict.common.noDataYet}</Pill>
            </div>
          </Card>

          <div className="grid gap-4 sm:grid-cols-3">
            <Stat
              label={dict.dashboard.weeklySets}
              value="20"
              unit={dict.common.sets}
              caption="+11%"
            />
            <Stat label={dict.dashboard.weeklyVolume} value="14 280" unit="kg" />
            <Stat label={dict.dashboard.lastSession} caption={dict.common.awaitingImport} />
          </div>
        </Section>

        <Section title={dict.template.emptyStates} lead={dict.template.emptyStatesLead}>
          <EmptyState title={dict.workouts.emptyTitle} body={dict.workouts.emptyBody} />
        </Section>

        <Section title={dict.template.stack} lead={dict.template.stackLead}>
          <Card>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[32rem] text-sm">
                <thead>
                  <tr className="border-b border-line text-left">
                    <th className="eyebrow pb-2">
                      {dict.template.choiceColumn}
                    </th>
                    <th className="eyebrow pb-2">
                      {dict.template.whyColumn}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {STACK.map((row) => (
                    <tr key={row.choice} className="border-b border-line last:border-0">
                      <td className="py-2.5 pr-4 font-medium whitespace-nowrap text-ink">
                        {row.choice}
                      </td>
                      <td className="py-2.5 text-ink-muted">{row.why}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </Section>
      </div>
    </>
  );
}

function Section({
  title,
  lead,
  children,
}: {
  title: string;
  lead: string;
  children: React.ReactNode;
}) {
  return (
    <section className="flex flex-col gap-4">
      <div>
        <h2 className="text-lg font-semibold text-ink">{title}</h2>
        <p className="mt-1 max-w-2xl text-sm text-ink-muted">{lead}</p>
      </div>
      {children}
    </section>
  );
}

function TokenTable({
  rows,
  tokenColumn,
  roleColumn,
}: {
  rows: Array<{ token: string; swatch: string; role: string }>;
  tokenColumn: string;
  roleColumn: string;
}) {
  return (
    <div className="overflow-x-auto rounded-lg border border-line bg-surface">
      <table className="w-full min-w-[32rem] text-sm">
        <thead>
          <tr className="border-b border-line text-left">
            <th className="w-14 py-2.5 pl-4" />
            <th className="eyebrow py-2.5">
              {tokenColumn}
            </th>
            <th className="eyebrow py-2.5 pr-4">
              {roleColumn}
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.token} className="border-b border-line last:border-0">
              <td className="py-2.5 pl-4">
                <span
                  aria-hidden
                  className={`block size-6 rounded border border-line-strong ${row.swatch}`}
                />
              </td>
              <td className="py-2.5 pr-4 font-mono text-xs whitespace-nowrap text-ink">
                {row.token}
              </td>
              <td className="py-2.5 pr-4 text-ink-muted">{row.role}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

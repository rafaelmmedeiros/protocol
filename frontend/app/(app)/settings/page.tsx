import { cookies } from "next/headers";

import { Card, CardHeader } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { API_URL } from "@/lib/api";
import { getDictionary, getLocale } from "@/lib/i18n";
import { LOCALES } from "@/lib/i18n/locales";
import { parseTheme, THEME_COOKIE, THEMES } from "@/lib/preferences";
import { HevyConnection } from "./hevy-connection";
import { SettingsForm, type Choice } from "./settings-form";

type Connection = { connected: boolean; connectedAt: string | null };

const LOCALE_NAMES: Record<(typeof LOCALES)[number], string> = {
  // A language is always named in itself: someone looking for their own language recognises
  // "Português" and will not recognise "Portuguese" if the page is currently in English.
  "en-US": "English (US)",
  "pt-BR": "Português (BR)",
};

export default async function SettingsPage() {
  const [dict, locale, cookieStore] = await Promise.all([getDictionary(), getLocale(), cookies()]);

  const response = await fetch(`${API_URL}/hevy/connection`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  // Whether an account is connected, and nothing else -- the API has no endpoint that returns
  // the key, so there is nothing else to read (ADR-014).
  const connection: Connection = response.ok
    ? ((await response.json()) as Connection)
    : { connected: false, connectedAt: null };
  const theme = parseTheme(cookieStore.get(THEME_COOKIE)?.value);

  const themes: Choice[] = THEMES.map((value) => ({
    value,
    label: dict.theme[value],
    testId: `theme-${value}`,
  }));

  const locales: Choice[] = LOCALES.map((value) => ({
    value,
    label: LOCALE_NAMES[value],
    testId: `locale-${value}`,
  }));

  return (
    <>
      <PageHeader title={dict.settings.title} lead={dict.settings.lead} />
      <SettingsForm
        themes={themes}
        locales={locales}
        currentTheme={theme}
        currentLocale={locale}
        strings={{
          appearance: dict.settings.appearance,
          themeLabel: dict.theme.label,
          themeDescription: dict.theme.description,
          languageLabel: dict.language.label,
          languageDescription: dict.language.description,
          save: dict.settings.save,
          saving: dict.settings.saving,
          saved: dict.settings.saved,
        }}
      />

      <Card className="mt-8">
        <CardHeader title={dict.hevy.connectionTitle} />
        <p className="mb-4 text-xs text-ink-muted">{dict.hevy.connectionLead}</p>
        <HevyConnection
          connected={connection.connected}
          strings={{
            apiKeyLabel: dict.hevy.apiKeyLabel,
            apiKeyHelp: dict.hevy.apiKeyHelp,
            connect: dict.hevy.connect,
            connecting: dict.hevy.connecting,
            connected: dict.hevy.connected,
            notConnected: dict.hevy.notConnected,
            connectedSince: connection.connectedAt
              ? dict.hevy.connectedSince(
                  new Date(connection.connectedAt).toLocaleDateString(locale),
                )
              : null,
            keyNeverShown: dict.hevy.keyNeverShown,
          }}
        />
      </Card>
    </>
  );
}

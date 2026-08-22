import { cookies } from "next/headers";

import { PageHeader } from "@/components/ui/page-header";
import { getDictionary, getLocale } from "@/lib/i18n";
import { LOCALES } from "@/lib/i18n/locales";
import { parseTheme, THEME_COOKIE, THEMES } from "@/lib/preferences";
import { SettingsForm, type Choice } from "./settings-form";

const LOCALE_NAMES: Record<(typeof LOCALES)[number], string> = {
  // A language is always named in itself: someone looking for their own language recognises
  // "Português" and will not recognise "Portuguese" if the page is currently in English.
  "en-US": "English (US)",
  "pt-BR": "Português (BR)",
};

export default async function SettingsPage() {
  const [dict, locale, cookieStore] = await Promise.all([getDictionary(), getLocale(), cookies()]);
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
    </>
  );
}

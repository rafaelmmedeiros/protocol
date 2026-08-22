import { cookies, headers } from "next/headers";

import { LOCALE_COOKIE } from "@/lib/preferences";
import { enUS, type Dictionary } from "./dictionaries/en-US";
import { ptBR } from "./dictionaries/pt-BR";
import { negotiateLocale, type Locale } from "./locales";

const DICTIONARIES: Record<Locale, Dictionary> = {
  "en-US": enUS,
  "pt-BR": ptBR,
};

/**
 * The locale for this request: the saved choice if there is one, otherwise what the browser
 * asked for. Server-side only, so the dictionaries never reach the browser's bundle -- a
 * Client Component receives the strings it needs as props instead. The `next/headers` import
 * is the guard: it fails to build if this module is ever pulled into a Client Component.
 */
export async function getLocale(): Promise<Locale> {
  const [cookieStore, headerList] = await Promise.all([cookies(), headers()]);

  return negotiateLocale(
    cookieStore.get(LOCALE_COOKIE)?.value,
    headerList.get("accept-language"),
  );
}

export async function getDictionary(): Promise<Dictionary> {
  return DICTIONARIES[await getLocale()];
}

export type { Dictionary };

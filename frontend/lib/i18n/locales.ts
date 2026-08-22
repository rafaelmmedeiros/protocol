/**
 * The supported locales, and how one is chosen for a request.
 *
 * Deliberately not the `app/[lang]/` routing the Next.js i18n guide describes: the locale here
 * is a user preference, not an address. Putting it in the path would buy shareable localised
 * URLs -- worth a lot to a public site, nothing to a signed-in training log -- at the cost of
 * a locale segment in every route and every test's URL assertion.
 */

export const LOCALES = ["en-US", "pt-BR"] as const;

export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "en-US";

export function isLocale(value: string | undefined | null): value is Locale {
  return typeof value === "string" && (LOCALES as readonly string[]).includes(value);
}

/**
 * A stored choice always wins. Without one, the browser's `Accept-Language` decides: an exact
 * match first, then the language subtag, so a browser asking for plain `pt` still gets pt-BR.
 */
export function negotiateLocale(
  storedChoice: string | undefined | null,
  acceptLanguage: string | undefined | null,
): Locale {
  if (isLocale(storedChoice)) return storedChoice;

  for (const tag of parseAcceptLanguage(acceptLanguage)) {
    const exact = LOCALES.find((locale) => locale.toLowerCase() === tag);
    if (exact) return exact;

    const bySubtag = LOCALES.find((locale) => locale.toLowerCase().split("-")[0] === tag.split("-")[0]);
    if (bySubtag) return bySubtag;
  }

  return DEFAULT_LOCALE;
}

/** Lower-cased language tags, most wanted first. Malformed entries are dropped, not thrown on. */
function parseAcceptLanguage(header: string | undefined | null): string[] {
  if (!header) return [];

  return header
    .split(",")
    .map((part) => {
      const [tag, ...parameters] = part.trim().split(";");
      const quality = parameters
        .map((parameter) => parameter.trim())
        .find((parameter) => parameter.startsWith("q="));
      const weight = quality ? Number.parseFloat(quality.slice(2)) : 1;
      return { tag: tag.trim().toLowerCase(), weight: Number.isFinite(weight) ? weight : 0 };
    })
    .filter((entry) => entry.tag.length > 0 && entry.tag !== "*" && entry.weight > 0)
    .sort((a, b) => b.weight - a.weight)
    .map((entry) => entry.tag);
}

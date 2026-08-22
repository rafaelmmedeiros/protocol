/**
 * The two preferences this tier owns: which theme to paint and which language to speak.
 *
 * Both live in a cookie rather than in `localStorage`, and the reason is the first paint.
 * A cookie arrives with the request, so the server can stamp `data-theme` and `lang` on the
 * HTML it emits and the browser paints it right the first time. Reading `localStorage` would
 * mean the page renders in the wrong theme and then corrects itself, which is a flash.
 *
 * They are not `httpOnly`: nothing here is a secret, and a future client-side toggle should
 * be able to read them.
 */

export const THEME_COOKIE = "protocol_theme";
export const LOCALE_COOKIE = "protocol_locale";

/** A year. A preference that expires is a preference the user has to set again. */
export const PREFERENCE_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

export const THEMES = ["system", "light", "dark"] as const;

/**
 * `system` is the default and is stored as the *absence* of `data-theme`, so the stylesheet's
 * `prefers-color-scheme` block decides. Only an explicit choice stamps the attribute.
 */
export type Theme = (typeof THEMES)[number];

export const DEFAULT_THEME: Theme = "system";

export function isTheme(value: string | undefined | null): value is Theme {
  return typeof value === "string" && (THEMES as readonly string[]).includes(value);
}

export function parseTheme(value: string | undefined | null): Theme {
  return isTheme(value) ? value : DEFAULT_THEME;
}

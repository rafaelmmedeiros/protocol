"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { isLocale } from "@/lib/i18n/locales";
import {
  LOCALE_COOKIE,
  PREFERENCE_COOKIE_MAX_AGE,
  parseTheme,
  THEME_COOKIE,
} from "@/lib/preferences";
import { getCurrentUser } from "@/lib/session";

export type PreferencesState = { saved: boolean };

export async function updatePreferences(
  _previous: PreferencesState,
  formData: FormData,
): Promise<PreferencesState> {
  // A Server Function answers a plain POST, not only this form. It gets the same session
  // check any endpoint would get.
  if (!(await getCurrentUser())) redirect("/login");

  const theme = parseTheme(formData.get("theme")?.toString());
  const localeInput = formData.get("locale")?.toString();
  const cookieStore = await cookies();

  const options = {
    path: "/",
    maxAge: PREFERENCE_COOKIE_MAX_AGE,
    sameSite: "lax",
    // Not `secure`: the deployment is http on localhost, and a secure cookie would be
    // dropped there, leaving the preference silently unsaved. It moves when the auth
    // cookie's own SameSite does, on the day this is served over https.
  } as const;

  cookieStore.set(THEME_COOKIE, theme, options);
  if (isLocale(localeInput)) cookieStore.set(LOCALE_COOKIE, localeInput, options);

  // The root layout reads both cookies to stamp `lang` and `data-theme`, so it has to
  // re-render for the change to reach the page that is already open.
  revalidatePath("/", "layout");

  return { saved: true };
}

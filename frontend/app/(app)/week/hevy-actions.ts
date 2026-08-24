"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { readApiError } from "@/lib/api-error";
import { getDictionary, getLocale } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

export type LoopState = {
  /** Already translated, both of them. The backend sends codes (standard 3). */
  message?: string;
  error?: string;
};

async function post(path: string, body: unknown): Promise<Response> {
  const cookieStore = await cookies();

  return fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
  });
}

async function sentenceFor(code: string | undefined): Promise<string> {
  const dict = await getDictionary();

  switch (code) {
    case "HevyNotConnected":
      return dict.hevy.notConnectedError;
    case "HevyUnreachable":
      return dict.hevy.unreachable;
    case "HevyRateLimited":
      return dict.hevy.rateLimited;
    case "HevyUnreadable":
      // Kept apart from "unreachable" on purpose: retrying cannot fix a shape we read wrong.
      return dict.hevy.unreadable;
    case "WeekAlreadyTrainedFrom":
      return dict.hevy.alreadyTrainedFrom;
    case "PushedRoutineMissing":
      return dict.hevy.routineMissing;
    case "ExerciseNotMappable":
      return dict.hevy.exerciseNotMappable;
    default:
      return dict.profileErrors.unknown;
  }
}

/**
 * Sends the week to Hevy as a folder of routines.
 *
 * Always explicit, never automatic: this writes into a surface the system cannot clean up
 * afterwards, because Hevy has no delete endpoint (ADR-017).
 *
 * The locale travels with the request because the routine's note is composed server-side, in
 * the user's language — the one piece of display text the backend writes, and only because it
 * goes to a third party rather than back to us (ADR-016).
 */
export async function pushWeek(_previous: LoopState, formData: FormData): Promise<LoopState> {
  if (!(await getCurrentUser())) redirect("/login");

  const weekId = formData.get("weekId")?.toString();
  if (!weekId) return {};

  const [locale, dict] = await Promise.all([getLocale(), getDictionary()]);
  const response = await post(`/hevy/weeks/${weekId}/push`, { locale });

  if (!response.ok) {
    const error = readApiError(await response.json().catch(() => null));
    return { error: await sentenceFor(error?.code) };
  }

  // Deliberately no revalidatePath here. A push stores routine identifiers that this screen
  // never renders and leaves the comparison untouched, so re-rendering would buy nothing -- and
  // it would cost the outcome message, which the re-render wipes out of the action's own state.
  return { message: dict.hevy.pushed };
}

/** Pulls what changed in Hevy since the last sync, and reports what arrived. */
export async function syncHevy(_previous: LoopState, _formData: FormData): Promise<LoopState> {
  if (!(await getCurrentUser())) redirect("/login");

  const response = await post("/hevy/sync", undefined);
  const dict = await getDictionary();

  if (!response.ok) {
    const error = readApiError(await response.json().catch(() => null));
    return { error: await sentenceFor(error?.code) };
  }

  const result = (await response.json()) as {
    imported: number;
    tombstoned: number;
    unmapped: number;
  };

  // Both screens change: the week grows a comparison, and equipment grows suggestions.
  revalidatePath("/week");
  revalidatePath("/equipment");

  return {
    message: dict.hevy.synced(result.imported, result.tombstoned, result.unmapped),
  };
}

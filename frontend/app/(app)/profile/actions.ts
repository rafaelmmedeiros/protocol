"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { readApiError } from "@/lib/api-error";
import { minutesToSeconds, secondsToMinutes } from "@/lib/duration";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

export type ProfileState = {
  saved: boolean;
  /**
   * A sentence this tier owns, already translated. The backend's code is turned into words
   * here rather than in the form, because a Server Function runs on the server where the
   * dictionary lives — and because a function cannot cross into a Client Component anyway.
   */
  error?: string;
};

/**
 * Writes the training profile.
 *
 * A Server Function rather than a fetch from the component: the browser only ever talks to this
 * origin, and this runs on the server where `API_URL` and the request's cookies both live. It
 * also answers a plain POST, not only this form, so it does the same session check any endpoint
 * would do.
 */
export async function saveProfile(_previous: ProfileState, formData: FormData): Promise<ProfileState> {
  if (!(await getCurrentUser())) redirect("/login");

  const cookieStore = await cookies();

  // Minutes on screen, seconds on the wire. The conversion happens here, at the render edge,
  // and never upstream (root standard 4).
  const minutes = Number(formData.get("durationMinutes"));
  const body = {
    goal: formData.get("goal")?.toString() ?? "",
    daysPerWeek: Number(formData.get("daysPerWeek")),
    sessionDurationSeconds: Number.isFinite(minutes) ? minutesToSeconds(minutes) : 0,
    // Empty means "no choice", which is a value and not an omission: it is how a user goes back
    // to whatever their frequency maps to (ADR-030).
    split: formData.get("split")?.toString() || null,
  };

  const response = await fetch(`${API_URL}/training/profile`, {
    method: "PUT",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify(body),
    cache: "no-store",
  });

  if (!response.ok) {
    return { saved: false, error: await sentenceFor(await response.json().catch(() => null)) };
  }

  // The page reads the profile server-side, so it has to re-render for the saved values to be
  // what a reload shows.
  revalidatePath("/profile");

  return { saved: true };
}

/**
 * Turns the backend's code into a sentence in the reader's language. The code is the contract
 * and the sentence is ours (root standard 3); showing the API's English would put untranslated
 * text in a pt-BR screen and would break the day anyone reworded it.
 *
 * The two bounded errors take their bounds from the response rather than from a constant here,
 * so the numbers TD-002 and TD-012 decided are never duplicated in this tier — and a record
 * that supersedes them moves the sentence with it.
 */
async function sentenceFor(body: unknown): Promise<string> {
  const dict = await getDictionary();
  const errors = dict.profileErrors;
  const error = readApiError(body);

  switch (error?.code) {
    case "GoalNotSupported":
      return errors.GoalNotSupported;
    case "SplitNotAdmitted":
      return errors.SplitNotAdmitted;
    case "ProfileNotFound":
      return errors.ProfileNotFound;
    case "FrequencyOutOfRange":
      return errors.FrequencyOutOfRange(error.min ?? 0, error.max ?? 0);
    case "DurationOutOfRange":
      // The backend bounds duration in seconds; this screen speaks minutes.
      return errors.DurationOutOfRange(
        secondsToMinutes(error.min ?? 0),
        secondsToMinutes(error.max ?? 0),
      );
    default:
      return errors.unknown;
  }
}

"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { readApiError } from "@/lib/api-error";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

export type GenerateState = {
  /** A sentence this tier owns, already translated. The backend only ever sends a code. */
  error?: string;
};

/**
 * Generates a week from the saved profile and stores it.
 *
 * Every call writes a new week rather than editing the last one (ADR-003), so this is safe to
 * press twice — what it is not is idempotent, and the screen says "generate again" rather than
 * "refresh" for that reason.
 */
export async function generateWeek(
  _previous: GenerateState,
  _formData: FormData,
): Promise<GenerateState> {
  if (!(await getCurrentUser())) redirect("/login");

  const cookieStore = await cookies();
  const response = await fetch(`${API_URL}/training/weeks`, {
    method: "POST",
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  if (!response.ok) {
    const dict = await getDictionary();
    const error = readApiError(await response.json().catch(() => null));

    return {
      error:
        error?.code === "ProfileNotFound"
          ? dict.profileErrors.ProfileNotFound
          : dict.profileErrors.unknown,
    };
  }

  revalidatePath("/week");
  return {};
}

/**
 * Swaps one slot for another exercise that trains the same thing. The API writes a new week
 * rather than editing this one (`ADR-012`), so the previous week stays readable — someone may
 * have trained it.
 */
export async function substitute(formData: FormData): Promise<void> {
  if (!(await getCurrentUser())) redirect("/login");

  const prescriptionId = formData.get("prescriptionId")?.toString();
  const exerciseId = formData.get("exerciseId")?.toString();
  if (!prescriptionId || !exerciseId) return;

  const cookieStore = await cookies();
  await fetch(`${API_URL}/training/weeks/current/prescriptions/${prescriptionId}/substitute`, {
    method: "POST",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify({ exerciseId }),
    cache: "no-store",
  });

  revalidatePath("/week");
}

/**
 * "Never prescribe me this again." Read-modify-write, because the API replaces the whole
 * preference set rather than patching it.
 */
export async function excludeExercise(formData: FormData): Promise<void> {
  if (!(await getCurrentUser())) redirect("/login");

  const exerciseId = formData.get("exerciseId")?.toString();
  if (!exerciseId) return;

  const cookieStore = await cookies();
  const headers = { "content-type": "application/json", cookie: cookieStore.toString() };

  const current = await fetch(`${API_URL}/training/preferences`, { headers, cache: "no-store" });
  if (!current.ok) return;

  const preferences = (await current.json()) as {
    excluded: { exerciseId: string }[];
    preferredVariants: { movementPattern: string; exerciseId: string }[];
  };

  const excluded = new Set(preferences.excluded.map((row) => row.exerciseId));
  excluded.add(exerciseId);

  await fetch(`${API_URL}/training/preferences`, {
    method: "PUT",
    headers,
    body: JSON.stringify({
      excludedExerciseIds: [...excluded],
      preferredVariants: preferences.preferredVariants,
    }),
    cache: "no-store",
  });

  // The exclusion changes what a *future* generation contains; the stored week is untouched
  // (ADR-003). Both screens re-read.
  revalidatePath("/week");
  revalidatePath("/equipment");
}

/**
 * Declares what happened to one session of the queue: trained, or passed over.
 *
 * Two actions rather than one with a parameter, for the same reason the API has two routes —
 * they are different statements and neither should be reachable by mistyping the other. Neither
 * writes anything into imported training (root standard 7); both move the queue.
 */
async function declare(sessionId: string, route: "done" | "skip"): Promise<void> {
  if (!(await getCurrentUser())) redirect("/login");

  const cookieStore = await cookies();
  await fetch(`${API_URL}/training/weeks/current/sessions/${sessionId}/${route}`, {
    method: "POST",
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  // The page reads the plan server-side, and what changed is which session is next — so unlike
  // the push control, this screen really does show something different afterwards.
  revalidatePath("/week");
}

export async function markSessionDone(formData: FormData): Promise<void> {
  await declare(formData.get("sessionId")?.toString() ?? "", "done");
}

export async function skipSession(formData: FormData): Promise<void> {
  await declare(formData.get("sessionId")?.toString() ?? "", "skip");
}

"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { getCurrentUser } from "@/lib/session";

export type EraseState = {
  /** Null until something has been attempted, so a fresh panel says nothing at all. */
  outcome: "erased" | "failed" | null;
  /** Already translated. The backend sends counts and codes; this tier owns every sentence. */
  message?: string;
};

/**
 * Erases everything belonging to the signed-in user (ADR-025).
 *
 * Development only: the endpoint exists only where `Development:AllowErase` is set, so this
 * action can 404 and that is the correct answer rather than a fault. The panel is not drawn at
 * all in that case -- this is the second line of defence, not the first.
 *
 * The confirmation travels as a field rather than being implied by the request, so nothing
 * reaches the destructive path by replaying a URL.
 */
export async function eraseEverything(
  _previous: EraseState,
  formData: FormData,
): Promise<EraseState> {
  if (!(await getCurrentUser())) redirect("/login");

  const confirmed = formData.get("confirmed") === "true";
  const cookieStore = await cookies();

  const response = await fetch(`${API_URL}/training/erase`, {
    method: "POST",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify({ confirmed }),
    cache: "no-store",
  });

  const dict = (await import("@/lib/i18n")).getDictionary;
  const strings = (await dict()).erase;

  if (!response.ok) {
    return { outcome: "failed", message: strings.failed };
  }

  // Every screen the loop passes through now reads differently, and each is cached separately.
  // Missing one leaves a stale profile or week on screen after the data behind it is gone --
  // which is exactly the confusion the counts in the log exist to prevent.
  revalidatePath("/settings");
  revalidatePath("/profile");
  revalidatePath("/equipment");
  revalidatePath("/week");

  return { outcome: "erased", message: strings.done };
}

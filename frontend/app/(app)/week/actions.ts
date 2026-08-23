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

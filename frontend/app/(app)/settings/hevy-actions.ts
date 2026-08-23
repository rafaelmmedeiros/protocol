"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { readApiError } from "@/lib/api-error";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

export type HevyConnectionState = {
  connected: boolean;
  /** Already translated. The backend sends a code and this tier owns the sentence (standard 3). */
  error?: string;
};

/**
 * Saves the user's Hevy key.
 *
 * The key travels one way and one way only: it is written here and never read back, because no
 * endpoint returns it (ADR-014). Nothing on this tier stores or echoes it either -- the form
 * field is cleared by the round trip, and the state this returns carries only whether it worked.
 */
export async function connectHevy(
  _previous: HevyConnectionState,
  formData: FormData,
): Promise<HevyConnectionState> {
  if (!(await getCurrentUser())) redirect("/login");

  const apiKey = formData.get("apiKey")?.toString() ?? "";
  const cookieStore = await cookies();

  const response = await fetch(`${API_URL}/hevy/connection`, {
    method: "PUT",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify({ apiKey }),
    cache: "no-store",
  });

  if (!response.ok) {
    const dict = await getDictionary();
    const error = readApiError(await response.json().catch(() => null));

    return {
      connected: false,
      error:
        error?.code === "HevyKeyInvalid"
          ? dict.hevy.keyInvalid
          : error?.code === "HevyUnreachable"
            ? dict.hevy.unreachable
            : dict.profileErrors.unknown,
    };
  }

  // The week screen changes shape once an account is connected: it grows the controls that push
  // and sync.
  revalidatePath("/settings");
  revalidatePath("/week");

  return { connected: true };
}

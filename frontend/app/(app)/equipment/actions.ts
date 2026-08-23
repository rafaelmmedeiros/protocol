"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { readApiError } from "@/lib/api-error";
import { getDictionary } from "@/lib/i18n";
import { getCurrentUser } from "@/lib/session";

export type EquipmentState = {
  saved: boolean;
  /** Already translated. The backend sends a code and this tier owns the sentence. */
  error?: string;
};

async function put(path: string, body: unknown): Promise<Response> {
  const cookieStore = await cookies();

  return fetch(`${API_URL}${path}`, {
    method: "PUT",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify(body),
    cache: "no-store",
  });
}

/** Saves the gym: every checked item, replacing whatever was there. */
export async function saveEquipment(
  _previous: EquipmentState,
  formData: FormData,
): Promise<EquipmentState> {
  if (!(await getCurrentUser())) redirect("/login");

  const items = formData.getAll("item").map((value) => value.toString());
  const response = await put("/training/equipment", { items });

  if (!response.ok) {
    const dict = await getDictionary();
    const error = readApiError(await response.json().catch(() => null));

    return {
      saved: false,
      error:
        error?.code === "EquipmentSetEmpty"
          ? dict.equipment.setEmpty
          : error?.code === "UnknownEquipmentItem"
            ? dict.equipment.unknownItem
            : dict.profileErrors.unknown,
    };
  }

  // Both screens read server-side, and the week is generated from this.
  revalidatePath("/equipment");
  revalidatePath("/week");

  return { saved: true };
}

/**
 * Removes one exclusion. The API replaces the whole set, so the current one is read first and
 * sent back without the exercise being allowed again.
 */
export async function allowExerciseAgain(formData: FormData): Promise<void> {
  if (!(await getCurrentUser())) redirect("/login");

  const exerciseId = formData.get("exerciseId")?.toString();
  if (!exerciseId) return;

  const cookieStore = await cookies();
  const current = await fetch(`${API_URL}/training/preferences`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  if (!current.ok) return;

  const preferences = (await current.json()) as {
    excluded: { exerciseId: string }[];
    preferredVariants: { movementPattern: string; exerciseId: string }[];
  };

  await put("/training/preferences", {
    excludedExerciseIds: preferences.excluded
      .map((row) => row.exerciseId)
      .filter((id) => id !== exerciseId),
    preferredVariants: preferences.preferredVariants,
  });

  revalidatePath("/equipment");
  revalidatePath("/week");
}

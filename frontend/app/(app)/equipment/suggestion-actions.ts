"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { API_URL } from "@/lib/api";
import { getCurrentUser } from "@/lib/session";

/**
 * Answers one suggestion.
 *
 * Accepting adds the item to the gym; declining changes the gym not at all and only stops the
 * offer returning on every sync (ADR-020). Nothing here can remove anything, because the
 * endpoint behind it has no path that does.
 */
export async function answerSuggestion(formData: FormData): Promise<void> {
  if (!(await getCurrentUser())) redirect("/login");

  const item = formData.get("item")?.toString();
  if (!item) return;

  const accepted = formData.get("accepted")?.toString() === "true";
  const cookieStore = await cookies();

  await fetch(`${API_URL}/training/equipment/suggestions`, {
    method: "POST",
    headers: { "content-type": "application/json", cookie: cookieStore.toString() },
    body: JSON.stringify({ item, accepted }),
    cache: "no-store",
  });

  // Accepting widens what the generator may draw from, so the week changes too.
  revalidatePath("/equipment");
  revalidatePath("/week");
}

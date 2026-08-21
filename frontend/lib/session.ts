import { cookies } from "next/headers";
import { API_URL, type CurrentUser } from "./api";

/**
 * Asks the backend who the caller is, forwarding the request's cookies. Returns null when
 * there is no valid session, so pages can decide between rendering and redirecting.
 */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  const cookieStore = await cookies();
  const response = await fetch(`${API_URL}/auth/me`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  if (response.status === 401) return null;
  if (!response.ok) throw new Error(`Backend answered ${response.status} for /auth/me`);

  return (await response.json()) as CurrentUser;
}

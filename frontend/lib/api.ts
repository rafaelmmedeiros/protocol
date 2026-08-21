/**
 * The backend's address as seen from the Next.js server. The browser never uses it: every
 * request goes through this app's own origin and is proxied by app/api/[...path]/route.ts,
 * which keeps the auth cookie first-party and removes CORS from the browser's path.
 */
export const API_URL = process.env.API_URL ?? "http://localhost:8080";

export type CurrentUser = {
  id: string;
  email: string;
};

import { defineConfig, devices } from "@playwright/test";

/**
 * The suite runs against an already-running stack -- `docker compose up` for the app, or a
 * local `npm run dev` plus the API. It never starts a server itself, so the same tests cover
 * both cases without a second code path.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3000",
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});

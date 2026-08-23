import { expect, test, type Page } from "@playwright/test";

/**
 * The first screen in this product that shows something the system decided by itself.
 *
 * Selectors are `data-testid` throughout: the visible text here is translated, and the session
 * kinds and day names change entirely between locales.
 */

const password = "Passw0rd!";
const uniqueEmail = () => `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@protocol.test`;

async function register(page: Page) {
  await page.goto("/login");
  await page.getByTestId("toggle-mode").click();
  await page.getByTestId("email").fill(uniqueEmail());
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

async function saveProfile(page: Page, days: string, minutes: string) {
  await page.goto("/profile");
  await page.getByTestId("profile-days").fill(days);
  await page.getByTestId("profile-duration").fill(minutes);

  // The Server Function's round trip, not the confirmation message -- see profile.spec.ts.
  const saved = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/profile"),
  );
  await page.getByTestId("profile-submit").click();
  await saved;
}

test("the week section is closed to an anonymous visitor", async ({ page }) => {
  await page.goto("/week");
  await expect(page).toHaveURL(/\/login$/);
});

test("a user with no profile is told what is missing rather than shown an error", async ({
  page,
}) => {
  await register(page);
  await page.getByTestId("nav-week").click();

  await expect(page).toHaveURL(/\/week$/);
  // The empty state, not a failure: the user has done nothing wrong.
  await expect(page.getByTestId("week-go-to-profile")).toBeVisible();
  await expect(page.getByTestId("week-generate")).toHaveCount(0);
});

test("a user with a profile and no week sees the empty state and can generate", async ({
  page,
}) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await expect(page.getByTestId("week-generate")).toBeVisible();
  await expect(page.getByTestId("week-sessions")).toHaveCount(0);
});

test("a generated week renders one session per training day, starting on Monday", async ({
  page,
}) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();

  // The acceptance criterion: the session count matches the profile's frequency.
  await expect(page.getByTestId("week-sessions")).toBeVisible();
  await expect(page.getByTestId("session-day")).toHaveCount(4);

  // Monday first, always -- and the day is rendered as a real date, so this reads the text
  // rather than an id. It is the one place the locale is pinned by the assertion itself.
  await expect(page.getByTestId("session-day").first()).toContainText(/Monday/i);

  await expect(page.getByTestId("prescription").first()).toBeVisible();
});

test("every prescription shows its sets, repetitions and rest", async ({ page }) => {
  await register(page);
  await saveProfile(page, "3", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  const volume = page.getByTestId("prescription-volume").first();
  const rest = page.getByTestId("prescription-rest").first();

  // "3 x 6-10" -- a set count and a repetition range, not a single number.
  await expect(volume).toContainText(/\d+\s*×\s*\d+–\d+/);
  // Rest is stored in seconds and rendered in minutes at this edge (root standard 4).
  await expect(rest).toContainText(/min|\bs\b/);
});

test("a week survives a full page load, and generating again replaces what is shown", async ({
  page,
}) => {
  await register(page);
  await saveProfile(page, "3", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("session-day")).toHaveCount(3);

  // A reload proves the week is in the database, not in client state.
  await page.reload();
  await expect(page.getByTestId("session-day")).toHaveCount(3);

  // The stored week keeps the frequency it was generated under; the new one picks up the
  // edited profile (ADR-003 snapshots the profile onto the week).
  await saveProfile(page, "5", "60");
  await page.goto("/week");
  await expect(page.getByTestId("session-day")).toHaveCount(3);

  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("session-day")).toHaveCount(5);
});

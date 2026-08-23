import { expect, test, type Page } from "@playwright/test";

/**
 * The loop, from the outside: connect an account, send the week, sync, and read what was
 * performed beside what was prescribed.
 *
 * The API container this drives has Hevy faked inside it — `Hevy__UseFake`, set only by
 * `docker-compose.test.yml`. Without that switch this suite would reach api.hevyapp.com, which
 * would mean depending on a third party's uptime, spending a real account's rate budget, and
 * keeping a real credential in CI.
 *
 * Selectors are `data-testid` throughout: every visible string here is translated.
 */

const password = "Passw0rd!";
const uniqueEmail = () => `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@protocol.test`;

/** Any key the fake accepts. It is never displayed again, which is what the suite checks. */
const goodKey = "e2e-hevy-key";

async function register(page: Page) {
  await page.goto("/login");
  await page.getByTestId("toggle-mode").click();
  await page.getByTestId("email").fill(uniqueEmail());
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

async function connect(page: Page, key: string) {
  await page.goto("/settings");
  await page.getByTestId("hevy-api-key").fill(key);

  const saved = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/settings"),
  );
  await page.getByTestId("hevy-connect").click();
  await saved;
}

async function generateWeek(page: Page) {
  await page.goto("/profile");
  await page.getByTestId("profile-days").fill("3");
  await page.getByTestId("profile-duration").fill("60");

  const savedProfile = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/profile"),
  );
  await page.getByTestId("profile-submit").click();
  await savedProfile;

  await page.goto("/week");
  const generated = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/week"),
  );
  await page.getByTestId("week-generate").click();
  await generated;
}

/**
 * Presses a control, waits for its round trip, and then for its outcome.
 *
 * Both waits are needed and neither is enough alone. The outcome of a *previous* press survives
 * on screen, so asserting on it straight after a click can pass without the second press having
 * happened at all — the failure `fillProfile` had in M1. And the round trip alone does not mean
 * the message is rendered yet. The subscription is opened before the click so a fast response
 * cannot be missed.
 */
async function press(page: Page, testId: string, outcome: string) {
  const done = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/week"),
  );
  await page.getByTestId(testId).click();
  await done;
  await expect(page.getByTestId(outcome)).toBeVisible({ timeout: 15_000 });
}

test("the loop is invisible until an account is connected", async ({ page }) => {
  await register(page);
  await generateWeek(page);

  // A week is readable without Hevy — M1 and M2 still work on their own.
  await expect(page.getByTestId("week-sessions")).toBeVisible();
  await expect(page.getByTestId("week-loop")).toHaveCount(0);
  await expect(page.getByTestId("comparison")).toHaveCount(0);
});

test("a key Hevy rejects is refused and nothing claims to be connected", async ({ page }) => {
  await register(page);

  // The fake rejects exactly this one, so the failure path is reachable from a browser.
  await connect(page, "invalid-key");

  await expect(page.getByTestId("hevy-error")).toBeVisible();
  await expect(page.getByTestId("hevy-saved")).toHaveCount(0);
});

test("the key is never shown again, not even after a reload", async ({ page }) => {
  await register(page);
  await connect(page, goodKey);

  await expect(page.getByTestId("hevy-saved")).toBeVisible();

  await page.reload();

  await expect(page.getByTestId("hevy-status")).toContainText(/./);
  // ADR-014: no endpoint returns the key, so nothing on this page can render it.
  await expect(page.getByTestId("hevy-api-key")).toHaveValue("");
  await expect(page.locator("body")).not.toContainText(goodKey);
});

test("connect, send, sync, and read what was performed", async ({ page }) => {
  await register(page);
  await connect(page, goodKey);
  await generateWeek(page);

  // The controls appear only once an account is connected.
  await expect(page.getByTestId("week-loop")).toBeVisible();

  await press(page, "week-push", "push-result");
  await press(page, "week-sync", "sync-result");

  // The deliverable of this milestone: the week reads back with what was logged against it.
  await expect(page.getByTestId("comparison")).toBeVisible();
  await expect(page.getByTestId("comparison-coverage")).toBeVisible();

  // Prescribed and performed on one screen, without navigating.
  const slots = page.locator('[data-testid^="comparison-slot-"]');
  await expect(slots.first()).toBeVisible();
});

test("sending the same week twice is allowed while nothing has trained from it", async ({
  page,
}) => {
  await register(page);
  await connect(page, goodKey);
  await generateWeek(page);

  await press(page, "week-push", "push-result");

  // ADR-017: re-pushing an untrained week replaces its routines rather than leaving litter in a
  // surface Hevy gives us no way to clean up.
  await press(page, "week-push", "push-result");
  await expect(page.getByTestId("push-error")).toHaveCount(0);
});

test("a week that has been trained from is refused rather than rewritten", async ({ page }) => {
  await register(page);
  await connect(page, goodKey);
  await generateWeek(page);

  await press(page, "week-push", "push-result");
  await press(page, "week-sync", "sync-result");

  // Now something has been logged against it, so its routines are evidence (ADR-017).
  await press(page, "week-push", "push-error");
});

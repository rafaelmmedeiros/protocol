import { expect, test, type Page } from "@playwright/test";

/**
 * The training profile: the first screen that writes something the generator will read.
 *
 * Every selector is a `data-testid`. Visible text here is translated, and selecting on it would
 * make the suite fail the moment someone switches the account to pt-BR.
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

async function fillProfile(page: Page, days: string, minutes: string) {
  await page.getByTestId("profile-days").fill(days);
  await page.getByTestId("profile-duration").fill(minutes);

  // Wait for the Server Function's own round trip, not for the confirmation message. The
  // message survives the previous save, so asserting on it passes instantly on a second submit
  // and lets a reload race the write that is still in flight.
  const saved = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/profile"),
  );
  await page.getByTestId("profile-submit").click();
  await saved;
}

test("the profile section is closed to an anonymous visitor", async ({ page }) => {
  await page.goto("/profile");
  await expect(page).toHaveURL(/\/login$/);
});

test("a profile is saved and survives a full page load", async ({ page }) => {
  await register(page);

  await page.getByTestId("nav-profile").click();
  await expect(page).toHaveURL(/\/profile$/);

  await fillProfile(page, "5", "45");
  await expect(page.getByTestId("profile-saved")).toBeVisible();

  // A reload proves the values are in the database, not in client state.
  await page.reload();
  await expect(page.getByTestId("profile-days")).toHaveValue("5");
  await expect(page.getByTestId("profile-duration")).toHaveValue("45");
});

test("an edited profile replaces the previous one rather than adding another", async ({ page }) => {
  await register(page);
  await page.goto("/profile");

  await fillProfile(page, "3", "40");
  await expect(page.getByTestId("profile-saved")).toBeVisible();

  await fillProfile(page, "6", "90");
  await expect(page.getByTestId("profile-saved")).toBeVisible();

  await page.reload();
  await expect(page.getByTestId("profile-days")).toHaveValue("6");
  await expect(page.getByTestId("profile-duration")).toHaveValue("90");
});

test("a frequency the product does not support is refused in the reader's language", async ({
  page,
}) => {
  await register(page);
  await page.goto("/profile");

  // The number input's own min/max are an affordance, not the authority -- the API is. Removing
  // the attribute is what a determined user (or a script) would do, and the backend still says no.
  await page.getByTestId("profile-days").evaluate((input) => input.removeAttribute("min"));
  await fillProfile(page, "1", "60");

  await expect(page.getByTestId("profile-error")).toBeVisible();
  // The sentence is this tier's, built from the code and the bounds the API sent back.
  await expect(page.getByTestId("profile-error")).toContainText("2");
  await expect(page.getByTestId("profile-error")).toContainText("6");
});

test("the goals this product does not programme yet are visible but not choosable", async ({
  page,
}) => {
  // ADR-004 collects the goal as a field and programmes one of its values. Surfacing the rest
  // as unavailable is the honest version of that.
  await register(page);
  await page.goto("/profile");

  const options = page.getByTestId("profile-goal").locator("option");
  await expect(options).toHaveCount(4);

  const disabled = await options.evaluateAll((nodes) =>
    nodes.filter((node) => (node as HTMLOptionElement).disabled).length,
  );
  expect(disabled).toBe(3);
});

test("a split can be chosen, and the choice is not the same thing as the default", async ({
  page,
}) => {
  await register(page);
  await page.goto("/profile");

  const split = page.getByTestId("profile-split");

  // Four days admits two templates, plus the empty option that means "whatever this frequency
  // maps to" — which is a real answer and not a placeholder (ADR-030).
  await expect(split.locator("option")).toHaveCount(3);
  await expect(split).toHaveValue("");

  await split.selectOption("PushPullLegsFull");

  const saved = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/profile"),
  );
  await page.getByTestId("profile-submit").click();
  await saved;

  await page.reload();
  await expect(page.getByTestId("profile-split")).toHaveValue("PushPullLegsFull");
});

test("the splits on offer follow the frequency in the form, not the saved one", async ({ page }) => {
  // The list belongs to a frequency and the frequency is edited on this screen. Two sessions
  // admits exactly one arrangement, so its select carries only the default option — which is
  // the case that proves the list is being filtered rather than fetched once (TD-023).
  await register(page);
  await page.goto("/profile");

  const split = page.getByTestId("profile-split");
  await expect(split.locator("option")).toHaveCount(3);

  await page.getByTestId("profile-days").fill("5");
  await expect(split.locator("option")).toHaveCount(3);
  await expect(split.locator("option")).toContainText([/.*/, /Upper/, /Upper/]);

  await page.getByTestId("profile-days").fill("2");
  await expect(split.locator("option")).toHaveCount(2);
});

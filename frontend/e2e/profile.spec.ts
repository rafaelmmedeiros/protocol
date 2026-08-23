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
  await page.getByTestId("profile-submit").click();
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

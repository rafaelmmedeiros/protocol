import { expect, test, type Page } from "@playwright/test";

/**
 * Describing a gym, and refusing an exercise. Selectors are `data-testid` throughout — every
 * label on these screens is translated.
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

/** Waits for the Server Function's round trip rather than for a message that survives it. */
async function submitAndWait(page: Page, testId: string, urlPart: string) {
  const done = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes(urlPart),
  );
  await page.getByTestId(testId).click();
  await done;
}

test("the equipment section is closed to an anonymous visitor", async ({ page }) => {
  await page.goto("/equipment");
  await expect(page).toHaveURL(/\/login$/);
});

test("a new user starts with the assumed gym already ticked", async ({ page }) => {
  // A user who never opens this screen behaves exactly as in M1, so the screen has to show
  // that state rather than an empty one.
  await register(page);
  await page.getByTestId("nav-equipment").click();

  await expect(page.getByTestId("equipment-Barbell")).toBeChecked();
  await expect(page.getByTestId("equipment-CableStation")).toBeChecked();
  await expect(page.getByTestId("equipment-Bench")).toBeChecked();
});

test("a described gym survives a full page load", async ({ page }) => {
  await register(page);
  await page.goto("/equipment");

  await page.getByTestId("equipment-CableStation").uncheck();
  await page.getByTestId("equipment-LatPulldownStation").uncheck();
  await submitAndWait(page, "equipment-submit", "/equipment");
  await expect(page.getByTestId("equipment-saved")).toBeVisible();

  await page.reload();
  await expect(page.getByTestId("equipment-CableStation")).not.toBeChecked();
  await expect(page.getByTestId("equipment-LatPulldownStation")).not.toBeChecked();
  await expect(page.getByTestId("equipment-Barbell")).toBeChecked();
});

test("a gym with nothing in it is refused in the reader's language", async ({ page }) => {
  await register(page);
  await page.goto("/equipment");

  for (const item of await page.locator('input[name="item"]').all()) {
    await item.uncheck();
  }
  await submitAndWait(page, "equipment-submit", "/equipment");

  await expect(page.getByTestId("equipment-error")).toBeVisible();
});

test("nothing is refused until the user refuses something", async ({ page }) => {
  await register(page);
  await page.goto("/equipment");

  await expect(page.getByTestId("excluded-empty")).toBeVisible();
});

test("a gym without cables never prescribes a cable exercise", async ({ page }) => {
  // The whole point of ADR-013, end to end and through the browser.
  await register(page);

  await page.goto("/profile");
  await page.getByTestId("profile-days").fill("3");
  await page.getByTestId("profile-duration").fill("60");
  await submitAndWait(page, "profile-submit", "/profile");

  await page.goto("/equipment");
  await page.getByTestId("equipment-CableStation").uncheck();
  await page.getByTestId("equipment-LatPulldownStation").uncheck();
  await submitAndWait(page, "equipment-submit", "/equipment");

  await page.goto("/week");
  await submitAndWait(page, "week-generate", "/week");

  await expect(page.getByTestId("week-sessions")).toBeVisible();
  const titles = await page.getByTestId("prescription").allInnerTexts();
  expect(titles.join(" ")).not.toMatch(/Cable|Pulldown/i);
  expect(titles.length).toBeGreaterThan(0);
});

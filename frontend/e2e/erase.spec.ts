import { expect, test, type Page } from "@playwright/test";

/**
 * Erasing everything of one user's, and starting the loop again from nothing (ADR-025).
 *
 * The endpoint exists here because docker-compose.test.yml sets `Development__AllowErase`, the
 * same shape `Hevy__UseFake` uses. Its *absence* when the switch is off cannot be proved from a
 * running container -- that is the integration suite's job, which can build a host with the flag
 * unset.
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

async function describeAWeek(page: Page) {
  await page.goto("/profile");
  await page.getByTestId("profile-days").fill("3");
  await page.getByTestId("profile-duration").fill("60");
  await submitAndWait(page, "profile-submit", "/profile");

  await page.goto("/week");
  await submitAndWait(page, "week-generate", "/week");
  await expect(page.getByTestId("week-sessions")).toBeVisible();
}

test("nothing is erased until it is confirmed", async ({ page }) => {
  // Deliberate or nothing. The first button only asks; backing out has to leave everything
  // exactly as it was, or the confirmation step is theatre.
  await register(page);
  await describeAWeek(page);

  await page.goto("/settings");
  await page.getByTestId("erase-start").click();
  await expect(page.getByTestId("erase-asking")).toBeVisible();
  await page.getByTestId("erase-cancel").click();
  await expect(page.getByTestId("erase-asking")).toHaveCount(0);

  await page.goto("/week");
  await expect(page.getByTestId("week-sessions")).toBeVisible();
});

test("erasing everything returns the product to a fresh account, and the loop runs again", async ({
  page,
}) => {
  await register(page);
  await describeAWeek(page);

  await page.goto("/settings");
  await page.getByTestId("erase-start").click();
  await submitAndWait(page, "erase-confirm", "/settings");
  await expect(page.getByTestId("erase-result")).toBeVisible();

  // Still signed in: the account is not "mine" in the sense ADR-025 means, so no screen bounces
  // to login. A redirect here would be the failure, not a 404.
  await page.goto("/profile");
  await expect(page.getByTestId("profile-submit")).toBeVisible();

  // And the whole point of the affordance: the loop runs again from nothing, which is what
  // "reset by hand" used to mean and what root standard 14 forbids doing to this database.
  await describeAWeek(page);
});

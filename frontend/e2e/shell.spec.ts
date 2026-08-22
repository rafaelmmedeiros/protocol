import { expect, test, type Page } from "@playwright/test";

/**
 * The app shell: navigation between the four sections, and the two preferences that have to
 * outlive a page load. Theme and language are read from a cookie on the server, so the proof
 * that they work is that a full reload comes back already correct -- not that a toggle
 * flipped a class after hydration.
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

test("the navigation reaches every section", async ({ page }) => {
  await register(page);

  for (const [testId, path] of [
    ["nav-workouts", "/workouts"],
    ["nav-adjustments", "/adjustments"],
    ["nav-template", "/template"],
    ["nav-dashboard", "/dashboard"],
  ] as const) {
    await page.getByTestId(testId).click();
    await expect(page).toHaveURL(new RegExp(`${path}$`));
    await expect(page.getByTestId(testId)).toHaveAttribute("aria-current", "page");
  }
});

test("the account menu reaches settings", async ({ page }) => {
  await register(page);

  await page.getByTestId("user-menu").click();
  await page.getByTestId("menu-settings").click();
  await expect(page).toHaveURL(/\/settings$/);
});

test("a chosen theme survives a reload", async ({ page }) => {
  await register(page);
  await page.goto("/settings");

  // No attribute at all is the third state: follow the operating system.
  await expect(page.locator("html")).not.toHaveAttribute("data-theme");

  await page.getByTestId("theme-dark").click();
  await page.getByTestId("save-preferences").click();
  await expect(page.getByTestId("preferences-saved")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");

  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");

  // And it holds across the app, not only on the page that set it.
  await page.getByTestId("nav-dashboard").click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
});

test("a chosen language translates the app and survives a reload", async ({ page }) => {
  await register(page);
  await page.goto("/settings");

  await expect(page.locator("html")).toHaveAttribute("lang", "en-US");
  await expect(page.getByTestId("nav-dashboard")).toHaveText("Dashboard");

  await page.getByTestId("locale-pt-BR").click();
  await page.getByTestId("save-preferences").click();

  await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");
  await expect(page.getByTestId("nav-dashboard")).toHaveText("Painel");

  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");
  await expect(page.getByTestId("nav-workouts")).toHaveText("Treinos");
});

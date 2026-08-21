import { expect, test } from "@playwright/test";

/**
 * The walking skeleton's end-to-end proof: a browser drives the Next.js app, which reaches the
 * .NET API, which persists to Postgres -- and the session survives a full page load.
 */

const password = "Passw0rd!";
const uniqueEmail = () => `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@protocol.test`;

test("an anonymous visitor is sent to the login page", async ({ page }) => {
  await page.goto("/dashboard");
  await expect(page).toHaveURL(/\/login$/);
});

test("a new account can register, land on the dashboard and sign out", async ({ page }) => {
  const email = uniqueEmail();

  await page.goto("/login");
  await page.getByTestId("toggle-mode").click();
  await page.getByTestId("email").fill(email);
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId("user-email")).toHaveText(email);

  // A reload proves the session lives in the cookie, not in client state.
  await page.reload();
  await expect(page.getByTestId("user-email")).toHaveText(email);

  await page.getByTestId("logout").click();
  await expect(page).toHaveURL(/\/login$/);

  await page.goto("/dashboard");
  await expect(page).toHaveURL(/\/login$/);
});

test("an existing account can sign in again", async ({ page }) => {
  const email = uniqueEmail();

  await page.goto("/login");
  await page.getByTestId("toggle-mode").click();
  await page.getByTestId("email").fill(email);
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await page.getByTestId("logout").click();
  await expect(page).toHaveURL(/\/login$/);

  await page.getByTestId("email").fill(email);
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId("user-email")).toHaveText(email);
});

test("a wrong password is reported and keeps the visitor on the login page", async ({ page }) => {
  const email = uniqueEmail();

  await page.goto("/login");
  await page.getByTestId("toggle-mode").click();
  await page.getByTestId("email").fill(email);
  await page.getByTestId("password").fill(password);
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await page.getByTestId("logout").click();

  await page.getByTestId("email").fill(email);
  await page.getByTestId("password").fill("Wrong1!");
  await page.getByTestId("submit").click();

  await expect(page.getByTestId("error")).toBeVisible();
  await expect(page).toHaveURL(/\/login$/);
});

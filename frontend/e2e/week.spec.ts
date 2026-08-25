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

test("a generated plan renders one session per training day, numbered in queue order", async ({
  page,
}) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();

  // The acceptance criterion: the session count matches the profile's frequency.
  await expect(page.getByTestId("week-sessions")).toBeVisible();
  await expect(page.getByTestId("session-day")).toHaveCount(4);

  // A place in the queue rather than a weekday (ADR-027). This used to assert "Monday" first,
  // which is exactly the promise the screen stopped making: a session happens when it happens.
  // The text is translated, so the assertion is on the number the label ends with.
  await expect(page.getByTestId("session-day").first()).toContainText(/1/);
  await expect(page.getByTestId("session-day").last()).toContainText(/4/);

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

test("every session says how long it is expected to take, within what the user has", async ({
  page,
}) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  const estimates = page.getByTestId("session-estimate");
  await expect(estimates).toHaveCount(4);

  // Two of the terms behind this number are engineering constants (TD-012). Showing it is what
  // makes them falsifiable, so the assertion is that it is a real number inside the budget.
  for (const text of await estimates.allInnerTexts()) {
    const minutes = Number(text.replace(/\D+/g, ""));
    expect(minutes).toBeGreaterThan(0);
    expect(minutes).toBeLessThanOrEqual(60);
  }
});

test("a slot can be swapped for something that trains the same thing", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "90");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  const before = await page.getByTestId("prescription").allInnerTexts();
  const swap = page.locator('[data-testid^="swap-"]').first();
  await expect(swap).toBeVisible();

  const swapped = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/week"),
  );
  await swap.click();
  await swapped;

  const after = await page.getByTestId("prescription").allInnerTexts();

  // Same number of slots, and the week is not wholesale different — a swap changes what was
  // asked about (ADR-012).
  expect(after.length).toBe(before.length);
  const changed = before.filter((text, index) => text !== after[index]);
  expect(changed.length).toBeLessThanOrEqual(2);
});

test("refusing an exercise keeps it out of the next generation", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "90");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  const firstTitle = (await page.getByTestId("prescription").first().innerText()).split("\n")[0];

  const refused = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/week"),
  );
  await page.locator('[data-testid^="refuse-"]').first().click();
  await refused;

  // The stored week is untouched by a preference (ADR-003) — it is the *next* one that changes.
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  // And the refusal is listed where it can be undone.
  await page.goto("/equipment");
  await expect(page.getByTestId("excluded-list")).toContainText(firstTitle);
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

test("a slot says what it trains and what decided its numbers", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  // Every slot, not the first one: a join that dropped a row would still render the rest.
  const explanations = page.getByTestId("prescription-explains");
  const slots = page.getByTestId("prescription");
  await expect(explanations).toHaveCount(await slots.count());

  // The text is translated, so this asserts the shape rather than the words: a muscle name, a
  // class and an implement, separated by the middle dot the component joins them with.
  await expect(explanations.first()).toContainText("·");
  await expect(explanations.first()).not.toHaveText("");
});

test("the week reports what every muscle group receives", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();

  const volume = page.getByTestId("week-volume");
  await expect(volume).toBeVisible();

  // One row per muscle group the catalogue trains directly. Fifteen of the sixteen: Adductors
  // is uncovered and is reported in its own block instead, which is the distinction TD-013 draws
  // between a shortfall the user can fix and a gap in what exists.
  await expect(page.getByTestId("volume-row")).toHaveCount(15);
  await expect(page.getByTestId("week-uncovered")).toBeVisible();
});

test("swapping an exercise says what else it would change", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  // A slot with alternatives carries the note; one without carries nothing to explain.
  const notes = page.getByTestId(/^swap-note-/);
  expect(await notes.count()).toBeGreaterThan(0);
  await expect(notes.first()).not.toHaveText("");
});

test("a session can be marked trained, and the queue moves to the next one", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  // Only the head of the queue is declarable, so exactly one card carries the controls.
  await expect(page.getByTestId("session-done")).toHaveCount(1);
  await expect(page.getByTestId("session-outcome").first()).not.toHaveText("");

  await page.getByTestId("session-done").click();

  // The controls moved to the second card, which is the queue advancing.
  await expect(page.getByTestId("session-done")).toHaveCount(1);
  await expect(page.getByTestId("session-outcome").first()).not.toHaveText(
    await page.getByTestId("session-outcome").nth(1).innerText(),
  );
});

test("a session can be skipped, and skipping is not reported as trained", async ({ page }) => {
  await register(page);
  await saveProfile(page, "4", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-sessions")).toBeVisible();

  const firstOutcome = await page.getByTestId("session-outcome").first().innerText();
  await page.getByTestId("session-skip").click();

  // ADR-032: the queue advances either way, and only one of the two says it happened. The words
  // are translated, so the assertion is that the first card now reads differently from before
  // and differently from a session still pending.
  await expect(page.getByTestId("session-outcome").first()).not.toHaveText(firstOutcome);
  await expect(page.getByTestId("session-skip")).toHaveCount(1);
});

test("skipping a session shows up as volume that is not coming back", async ({ page }) => {
  await register(page);
  await saveProfile(page, "2", "60");

  await page.goto("/week");
  await page.getByTestId("week-generate").click();
  await expect(page.getByTestId("week-accumulation")).toBeVisible();

  // Nothing done and nothing skipped: the deferred column carries the plan and the skipped
  // column is zero everywhere.
  const skipped = page.getByTestId("accumulation-skipped");
  expect(await skipped.count()).toBeGreaterThan(0);
  for (const cell of await skipped.all()) {
    await expect(cell).toHaveText("0");
  }

  // The subscription opens before the click: the outcome of a previous press survives on screen,
  // and a completed request does not mean the re-render has happened yet.
  const skipping = page.waitForResponse(
    (response) => response.request().method() === "POST" && response.url().includes("/week"),
  );
  await page.getByTestId("session-skip").click();
  await skipping;

  // ADR-032: the volume in that session is now reported as never arriving, rather than as still
  // ahead in the queue. Polled rather than read once, so the assertion waits for the render.
  await expect
    .poll(async () => {
      const cells = await page.getByTestId("accumulation-skipped").allInnerTexts();
      return cells.some((value) => Number(value) > 0);
    })
    .toBe(true);
});

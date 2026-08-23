/**
 * Turning a stored week into dates a person can read.
 *
 * The API sends a week's Monday as a plain date and each session's day as a stable name, never
 * as a translated word (root standard 3). This resolves the two into a real date so the screen
 * can show "Monday 24 August" in the reader's language through `Intl`, rather than carrying
 * seven weekday strings per locale in the dictionary.
 */

/**
 * Days from the week's Monday. Monday is zero because the training week starts on Monday,
 * always, and never derives that from locale — an `en-US` calendar starting on Sunday must not
 * redraw the boundaries of a block (root standard 6).
 */
const OFFSET_FROM_MONDAY: Record<string, number> = {
  Monday: 0,
  Tuesday: 1,
  Wednesday: 2,
  Thursday: 3,
  Friday: 4,
  Saturday: 5,
  Sunday: 6,
};

/**
 * The date a session falls on.
 *
 * Built in UTC and formatted in UTC on purpose. `new Date("2026-08-24")` is midnight UTC, and
 * formatting that in a timezone behind UTC would render the day before — a week that silently
 * starts on Sunday for anyone west of Greenwich.
 */
export function sessionDate(weekStartDate: string, day: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(weekStartDate);
  const offset = OFFSET_FROM_MONDAY[day];
  if (!match || offset === undefined) return null;

  const [, year, month, dayOfMonth] = match;
  return new Date(Date.UTC(Number(year), Number(month) - 1, Number(dayOfMonth) + offset));
}

/** "Monday, 24 August" in the reader's language, or the stable name if the date will not parse. */
export function formatSessionDay(weekStartDate: string, day: string, locale: string): string {
  const date = sessionDate(weekStartDate, day);
  if (!date) return day;

  return new Intl.DateTimeFormat(locale, {
    weekday: "long",
    day: "numeric",
    month: "long",
    timeZone: "UTC",
  }).format(date);
}

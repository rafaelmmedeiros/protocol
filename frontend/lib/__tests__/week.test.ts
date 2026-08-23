import { describe, expect, it } from "vitest";

import { formatSessionDay, sessionDate } from "../week";

/** 2026-08-24 is a Monday. */
const MONDAY = "2026-08-24";

describe("sessionDate", () => {
  it("places every day of the training week after its Monday", () => {
    expect(sessionDate(MONDAY, "Monday")?.toISOString()).toBe("2026-08-24T00:00:00.000Z");
    expect(sessionDate(MONDAY, "Thursday")?.toISOString()).toBe("2026-08-27T00:00:00.000Z");
    expect(sessionDate(MONDAY, "Saturday")?.toISOString()).toBe("2026-08-29T00:00:00.000Z");
    // Sunday is the *last* day of a training week, not the first (root standard 6).
    expect(sessionDate(MONDAY, "Sunday")?.toISOString()).toBe("2026-08-30T00:00:00.000Z");
  });

  it("crosses a month boundary without drifting", () => {
    expect(sessionDate("2026-08-31", "Saturday")?.toISOString()).toBe("2026-09-05T00:00:00.000Z");
  });

  it("returns null rather than guessing at input it does not recognise", () => {
    expect(sessionDate("not-a-date", "Monday")).toBeNull();
    expect(sessionDate(MONDAY, "Caturday")).toBeNull();
  });
});

describe("formatSessionDay", () => {
  it("names the day in the reader's language", () => {
    expect(formatSessionDay(MONDAY, "Monday", "en-US")).toContain("Monday");
    expect(formatSessionDay(MONDAY, "Monday", "pt-BR").toLowerCase()).toContain("segunda");
  });

  it("does not shift the day for a reader behind UTC", () => {
    // The failure this guards against: a date built at midnight UTC and formatted in a local
    // zone renders the day before, so the training week silently starts on Sunday for anyone
    // west of Greenwich.
    expect(formatSessionDay(MONDAY, "Monday", "en-US")).toContain("24");
  });

  it("falls back to the stable name when the date will not parse", () => {
    expect(formatSessionDay("", "Monday", "en-US")).toBe("Monday");
  });
});

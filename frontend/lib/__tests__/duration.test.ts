import { describe, expect, it } from "vitest";

import { minutesToSeconds, secondsToMinutes, splitDuration } from "../duration";

describe("duration", () => {
  it("converts the supported range in both directions", () => {
    // The bounds TD-012 decided, as the profile screen presents them.
    expect(secondsToMinutes(1_500)).toBe(25);
    expect(secondsToMinutes(3_600)).toBe(60);
    expect(secondsToMinutes(7_200)).toBe(120);

    expect(minutesToSeconds(25)).toBe(1_500);
    expect(minutesToSeconds(60)).toBe(3_600);
    expect(minutesToSeconds(120)).toBe(7_200);
  });

  it("round-trips every whole minute in the supported range", () => {
    for (let minutes = 25; minutes <= 120; minutes += 1) {
      expect(secondsToMinutes(minutesToSeconds(minutes))).toBe(minutes);
    }
  });

  it("rounds a duration that is not a whole minute rather than truncating it", () => {
    // 50 minutes and 40 seconds is closer to 51 than to 50. Truncating would quietly shorten
    // every session that did not divide evenly.
    expect(secondsToMinutes(3_040)).toBe(51);
    expect(secondsToMinutes(3_029)).toBe(50);
  });

  it("splits the rest intervals TD-011 prescribes into minutes and seconds", () => {
    expect(splitDuration(180)).toEqual({ minutes: 3, seconds: 0 });
    expect(splitDuration(150)).toEqual({ minutes: 2, seconds: 30 });
    expect(splitDuration(90)).toEqual({ minutes: 1, seconds: 30 });
  });

  it("never returns a negative or fractional split", () => {
    expect(splitDuration(-10)).toEqual({ minutes: 0, seconds: 0 });
    expect(splitDuration(90.4)).toEqual({ minutes: 1, seconds: 30 });
  });
});

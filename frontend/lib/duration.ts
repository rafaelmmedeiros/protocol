/**
 * Duration crosses the wire in seconds and reaches a person in minutes.
 *
 * The domain never holds minutes: `session_duration_seconds` carries its unit in the name, and
 * the conversion happens here, at the render edge, because that is the only place a rendered
 * unit belongs (root standard 4). A minute that leaked upstream would be a unit the backend has
 * to guess at.
 */

/** Seconds as a whole number of minutes, for display and for form defaults. */
export function secondsToMinutes(seconds: number): number {
  return Math.round(seconds / 60);
}

/** Minutes back into the canonical unit, for anything sent to the API. */
export function minutesToSeconds(minutes: number): number {
  return Math.round(minutes * 60);
}

/**
 * Splits a rest interval into whole minutes and the remainder, so a screen can write "1 min
 * 30 s" instead of "90 s". The words themselves live in the dictionary; this only does the
 * arithmetic, which is why it is testable without a locale.
 */
export function splitDuration(totalSeconds: number): { minutes: number; seconds: number } {
  const safe = Math.max(0, Math.round(totalSeconds));
  return { minutes: Math.floor(safe / 60), seconds: safe % 60 };
}

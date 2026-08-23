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

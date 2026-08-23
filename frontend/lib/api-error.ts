/**
 * This product's own error shape, which is not Identity's.
 *
 * `lib/problem.ts` reads ASP.NET Core Identity's ProblemDetails, whose `errors` map is keyed by
 * a code. Our own endpoints answer something narrower and more deliberate: a stable code plus
 * whatever data the sentence needs (root standard 3). `FrequencyOutOfRange` travels with the
 * bounds it was rejected against, so this tier can say "between 2 and 6 days" without copying
 * the numbers TD-002 and TD-012 decided into a dictionary where they would drift.
 */
export type ApiError = {
  code: string;
  min?: number | null;
  max?: number | null;
};

/** Reads our error shape out of a response body, or null when it is not one. */
export function readApiError(body: unknown): ApiError | null {
  if (typeof body !== "object" || body === null) return null;

  const { code, min, max } = body as Partial<ApiError>;
  if (typeof code !== "string" || code.length === 0) return null;

  return {
    code,
    min: typeof min === "number" ? min : null,
    max: typeof max === "number" ? max : null,
  };
}

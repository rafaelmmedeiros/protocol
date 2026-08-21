/**
 * ASP.NET Core Identity reports validation failures as a ProblemDetails body carrying an
 * `errors` map. This lifts the first message out of it so the UI can show something specific.
 */
export type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export function firstProblemMessage(problem: unknown, fallback: string): string {
  if (typeof problem !== "object" || problem === null) return fallback;

  const { errors, detail, title } = problem as ProblemDetails;
  const fromErrors = errors && Object.values(errors).flat().find((message) => message.length > 0);

  return fromErrors ?? detail ?? title ?? fallback;
}

/**
 * ASP.NET Core Identity reports failures as a ProblemDetails body whose `errors` map is keyed
 * by a stable error code -- `PasswordTooShort`, `DuplicateUserName` -- with the English
 * sentence as the value.
 *
 * The code is the contract; the sentence is not. Standard 3 in the root CLAUDE.md says the
 * backend returns codes and this tier owns every translated string, so only the key is read
 * here. Showing the backend's sentence would put untranslated English in a pt-BR screen and
 * would break the moment anyone reworded it.
 */
export type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

/** The first error code in the body, or null when it is not a ProblemDetails we recognise. */
export function firstProblemCode(problem: unknown): string | null {
  if (typeof problem !== "object" || problem === null) return null;

  const { errors } = problem as ProblemDetails;
  if (!errors) return null;

  return Object.keys(errors).find((code) => code.length > 0) ?? null;
}

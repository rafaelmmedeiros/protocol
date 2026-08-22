/** Joins class names, dropping anything falsy. A dependency would buy nothing over this. */
export function cn(...parts: Array<string | false | null | undefined>): string {
  return parts.filter(Boolean).join(" ");
}

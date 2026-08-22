import { describe, expect, it } from "vitest";
import { firstProblemCode } from "../problem";

describe("firstProblemCode", () => {
  it("reads the error code, never the backend's sentence", () => {
    const problem = {
      title: "One or more validation errors occurred.",
      errors: { PasswordTooShort: ["Passwords must be at least 6 characters."] },
    };

    expect(firstProblemCode(problem)).toBe("PasswordTooShort");
  });

  it("returns null for a body with no error map", () => {
    expect(firstProblemCode({ detail: "Account locked." })).toBeNull();
    expect(firstProblemCode({ errors: {} })).toBeNull();
  });

  it("returns null for anything it cannot read", () => {
    expect(firstProblemCode(null)).toBeNull();
    expect(firstProblemCode("not json")).toBeNull();
  });
});

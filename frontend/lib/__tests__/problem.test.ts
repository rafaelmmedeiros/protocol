import { describe, expect, it } from "vitest";
import { firstProblemMessage } from "../problem";

describe("firstProblemMessage", () => {
  it("prefers the first validation error", () => {
    const problem = {
      title: "One or more validation errors occurred.",
      errors: { PasswordTooShort: ["Passwords must be at least 6 characters."] },
    };

    expect(firstProblemMessage(problem, "fallback")).toBe(
      "Passwords must be at least 6 characters.",
    );
  });

  it("falls back through detail and title when there are no field errors", () => {
    expect(firstProblemMessage({ detail: "Account locked." }, "fallback")).toBe("Account locked.");
    expect(firstProblemMessage({ title: "Bad request." }, "fallback")).toBe("Bad request.");
  });

  it("uses the fallback for anything it cannot read", () => {
    expect(firstProblemMessage(null, "fallback")).toBe("fallback");
    expect(firstProblemMessage("not json", "fallback")).toBe("fallback");
    expect(firstProblemMessage({ errors: {} }, "fallback")).toBe("fallback");
  });
});

import { describe, expect, it } from "vitest";

import { enUS } from "../dictionaries/en-US";
import { ptBR } from "../dictionaries/pt-BR";
import { negotiateLocale } from "../locales";

describe("negotiateLocale", () => {
  it("lets a saved choice win over the browser", () => {
    expect(negotiateLocale("pt-BR", "en-US,en;q=0.9")).toBe("pt-BR");
    expect(negotiateLocale("en-US", "pt-BR,pt;q=0.9")).toBe("en-US");
  });

  it("ignores a stored value that is not a supported locale", () => {
    expect(negotiateLocale("kl-GL", "pt-BR")).toBe("pt-BR");
    expect(negotiateLocale("", null)).toBe("en-US");
  });

  it("matches the browser on the language subtag, so plain pt gets pt-BR", () => {
    expect(negotiateLocale(null, "pt")).toBe("pt-BR");
    expect(negotiateLocale(null, "pt-PT,pt;q=0.9")).toBe("pt-BR");
  });

  it("honours quality weights rather than header order", () => {
    expect(negotiateLocale(null, "de;q=0.9,pt-BR;q=1.0")).toBe("pt-BR");
    expect(negotiateLocale(null, "pt-BR;q=0.2,en-US;q=0.8")).toBe("en-US");
  });

  it("falls back to en-US when nothing matches or the header is junk", () => {
    expect(negotiateLocale(null, "de,fr;q=0.8")).toBe("en-US");
    expect(negotiateLocale(null, ";;;")).toBe("en-US");
    expect(negotiateLocale(null, "*")).toBe("en-US");
  });
});

describe("the dictionaries", () => {
  // The compiler already enforces this through the `Dictionary` type. The test exists for the
  // case the type is ever widened, and to catch a group left filled with English placeholders.
  it("agree on every key", () => {
    expect(keysOf(ptBR)).toEqual(keysOf(enUS));
  });

  it("say something different in pt-BR", () => {
    expect(ptBR.nav.dashboard).not.toBe(enUS.nav.dashboard);
    expect(ptBR.login.invalidCredentials).not.toBe(enUS.login.invalidCredentials);
  });
});

function keysOf(value: object, prefix = ""): string[] {
  return Object.entries(value)
    .flatMap(([key, child]) =>
      typeof child === "object" && child !== null
        ? keysOf(child as object, `${prefix}${key}.`)
        : [`${prefix}${key}`],
    )
    .sort();
}

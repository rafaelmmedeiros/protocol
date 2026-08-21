import { defineConfig } from "vitest/config";

// e2e/ holds Playwright specs, which Vitest's default `**/*.spec.ts` glob would otherwise
// collect and fail on.
export default defineConfig({
  test: {
    include: ["lib/**/*.test.ts", "app/**/*.test.ts?(x)"],
    exclude: ["node_modules", ".next", "e2e"],
  },
});

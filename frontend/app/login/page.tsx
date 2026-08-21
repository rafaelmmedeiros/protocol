"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { firstProblemMessage } from "@/lib/problem";

type Mode = "login" | "register";

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<Mode>("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setPending(true);
    setError(null);

    try {
      if (mode === "register") {
        const registered = await fetch("/api/auth/register", {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify({ email, password }),
        });
        if (!registered.ok) {
          setError(await describeFailure(registered, "Could not create the account."));
          return;
        }
      }

      const signedIn = await fetch("/api/auth/login?useCookies=true", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (!signedIn.ok) {
        setError("Email or password is incorrect.");
        return;
      }

      router.replace("/dashboard");
      router.refresh();
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-sm flex-col justify-center gap-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">
          {mode === "login" ? "Sign in" : "Create an account"}
        </h1>
        <p className="mt-2 text-sm text-black/60 dark:text-white/60">Protocol</p>
      </div>

      <form onSubmit={submit} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm">
          Email
          <input
            type="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            data-testid="email"
            autoComplete="email"
            className="rounded-md border border-black/15 px-3 py-2 dark:border-white/20 dark:bg-transparent"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          Password
          <input
            type="password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            data-testid="password"
            autoComplete={mode === "login" ? "current-password" : "new-password"}
            className="rounded-md border border-black/15 px-3 py-2 dark:border-white/20 dark:bg-transparent"
          />
        </label>

        {error && (
          <p data-testid="error" role="alert" className="text-sm text-red-600 dark:text-red-400">
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={pending}
          data-testid="submit"
          className="rounded-md bg-foreground px-4 py-2 text-sm font-medium text-background disabled:opacity-50"
        >
          {pending ? "Working..." : mode === "login" ? "Sign in" : "Create account"}
        </button>
      </form>

      <button
        type="button"
        data-testid="toggle-mode"
        onClick={() => {
          setMode(mode === "login" ? "register" : "login");
          setError(null);
        }}
        className="text-sm underline underline-offset-4"
      >
        {mode === "login" ? "Create an account" : "I already have an account"}
      </button>
    </main>
  );
}

/** Reads the backend's failure body, falling back when it is not a ProblemDetails. */
async function describeFailure(response: Response, fallback: string): Promise<string> {
  try {
    return firstProblemMessage(await response.json(), fallback);
  } catch {
    return fallback;
  }
}

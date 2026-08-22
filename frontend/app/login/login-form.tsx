"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { firstProblemCode } from "@/lib/problem";

type Mode = "login" | "register";

export type LoginStrings = {
  signInTitle: string;
  registerTitle: string;
  lead: string;
  email: string;
  password: string;
  signIn: string;
  register: string;
  working: string;
  needAccount: string;
  haveAccount: string;
  invalidCredentials: string;
  registerFailed: string;
};

export function LoginForm({
  strings,
  authErrors,
}: {
  strings: LoginStrings;
  authErrors: Record<string, string>;
}) {
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
          setError(await translateFailure(registered, authErrors, strings.registerFailed));
          return;
        }
      }

      const signedIn = await fetch("/api/auth/login?useCookies=true", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (!signedIn.ok) {
        setError(strings.invalidCredentials);
        return;
      }

      router.replace("/dashboard");
      router.refresh();
    } finally {
      setPending(false);
    }
  }

  return (
    <>
      {/* The heading names the mode, so it lives here with the state rather than in the page. */}
      <h1 className="mt-3 text-2xl font-semibold text-ink">
        {mode === "login" ? strings.signInTitle : strings.registerTitle}
      </h1>
      <p className="mt-2 text-sm text-ink-muted">{strings.lead}</p>

      <form onSubmit={submit} className="mt-8 flex flex-col gap-4">
        <Field
          label={strings.email}
          type="email"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          data-testid="email"
          autoComplete="email"
        />
        <Field
          label={strings.password}
          type="password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          data-testid="password"
          autoComplete={mode === "login" ? "current-password" : "new-password"}
        />

        {error && (
          <p data-testid="error" role="alert" className="text-sm text-bad">
            {error}
          </p>
        )}

        <Button type="submit" disabled={pending} data-testid="submit" className="mt-2">
          {pending ? strings.working : mode === "login" ? strings.signIn : strings.register}
        </Button>
      </form>

      <button
        type="button"
        data-testid="toggle-mode"
        onClick={() => {
          setMode(mode === "login" ? "register" : "login");
          setError(null);
        }}
        className="mt-6 text-sm text-accent-ink underline-offset-4 hover:underline"
      >
        {mode === "login" ? strings.needAccount : strings.haveAccount}
      </button>
    </>
  );
}

/**
 * Turns the backend's failure into a sentence this tier owns. Only the error code is read --
 * an unknown code falls back to the generic message rather than leaking English from the API.
 */
async function translateFailure(
  response: Response,
  authErrors: Record<string, string>,
  fallback: string,
): Promise<string> {
  try {
    const code = firstProblemCode(await response.json());
    return (code && authErrors[code]) || fallback;
  } catch {
    return fallback;
  }
}

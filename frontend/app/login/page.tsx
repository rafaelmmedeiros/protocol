import { getDictionary } from "@/lib/i18n";
import { LoginForm } from "./login-form";

/**
 * A Server Component so the dictionary is read on the server; the form itself is a Client
 * Component and receives only the strings it shows. Whether it says "Sign in" or "Entrar" is
 * decided before the HTML leaves, from the browser's Accept-Language on a first visit.
 */
export default async function LoginPage() {
  const dict = await getDictionary();

  return (
    <main className="mx-auto flex w-full max-w-sm grow flex-col justify-center px-6 py-16">
      <p className="eyebrow text-accent-ink">
        {dict.app.name}
      </p>
      <LoginForm strings={dict.login} authErrors={dict.authErrors} />
    </main>
  );
}

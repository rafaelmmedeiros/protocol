import { redirect } from "next/navigation";
import { getCurrentUser } from "@/lib/session";
import { LogoutButton } from "./logout-button";

export default async function DashboardPage() {
  const user = await getCurrentUser();
  if (!user) redirect("/login");

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Signed in</h1>
        <p className="mt-2 text-sm text-black/60 dark:text-white/60">
          This page is rendered on the server and only reachable with a valid session.
        </p>
      </div>
      <dl className="rounded-lg border border-black/10 p-4 text-sm dark:border-white/15">
        <dt className="text-black/50 dark:text-white/50">Email</dt>
        <dd data-testid="user-email" className="font-medium">{user.email}</dd>
        <dt className="mt-3 text-black/50 dark:text-white/50">User id</dt>
        <dd className="font-mono text-xs break-all">{user.id}</dd>
      </dl>
      <LogoutButton />
    </main>
  );
}

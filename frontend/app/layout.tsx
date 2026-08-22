import type { Metadata } from "next";
import { cookies } from "next/headers";

import { getLocale } from "@/lib/i18n";
import { parseTheme, THEME_COOKIE } from "@/lib/preferences";
import "./globals.css";

export const metadata: Metadata = {
  title: "Protocol",
  description: "Training intelligence for what Hevy logs",
};

/**
 * Both the language and the theme are decided here, on the server, from cookies that arrive
 * with the request. That is the whole reason they are cookies: the first HTML already carries
 * `lang` and `data-theme`, so nothing renders in the wrong language or the wrong theme and
 * then corrects itself. `system` is stamped as no attribute at all, which hands the decision
 * to the stylesheet's `prefers-color-scheme` block.
 */
export default async function RootLayout({ children }: LayoutProps<"/">) {
  const [locale, cookieStore] = await Promise.all([getLocale(), cookies()]);
  const theme = parseTheme(cookieStore.get(THEME_COOKIE)?.value);

  return (
    <html
      lang={locale}
      data-theme={theme === "system" ? undefined : theme}
      className="h-full antialiased"
    >
      <body className="flex min-h-full flex-col">{children}</body>
    </html>
  );
}

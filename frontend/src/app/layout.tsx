import "./globals.css";

import { GeistMono } from "geist/font/mono";
import { GeistSans } from "geist/font/sans";
import type { Metadata, Viewport } from "next";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getTranslations } from "next-intl/server";

import { Providers } from "@/components/providers";
import { INTL_LOCALES } from "@/i18n/config";
import { getServerSession } from "@/lib/auth/session.server";
import { getServerCurrency, loadExchangeRates } from "@/lib/currency.server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("metadata");
  return {
    title: {
      default: t("title"),
      template: "%s · Lodge",
    },
    description: t("description"),
    applicationName: "Lodge",
    keywords: t("keywords").split(",").map((k) => k.trim()),
    openGraph: {
      title: t("title"),
      description: t("ogDescription"),
      type: "website",
      siteName: "Lodge",
      locale: INTL_LOCALES[await getLocale()].replace("-", "_"),
    },
  };
}

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#fcfcfa" },
    { media: "(prefers-color-scheme: dark)", color: "#14161b" },
  ],
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  // Decoding the cookie on the server lets the navbar render the right state
  // on first paint (no flash, no hydration mismatch).
  const [session, locale, t, currency, rates] = await Promise.all([
    getServerSession(),
    getLocale(),
    getTranslations("common"),
    getServerCurrency(),
    loadExchangeRates(),
  ]);

  return (
    <html
      lang={locale}
      suppressHydrationWarning
      className={`${GeistSans.variable} ${GeistMono.variable}`}
    >
      <body className="min-h-dvh font-sans">
        <a
          href="#main"
          className="sr-only z-[100] rounded-lg bg-primary px-4 py-2 text-primary-foreground focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
        >
          {t("skipToContent")}
        </a>
        {/* Inherits locale and messages from src/i18n/request.ts. */}
        <NextIntlClientProvider>
          <Providers initialSession={session} initialCurrency={currency} initialRates={rates}>{children}</Providers>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}

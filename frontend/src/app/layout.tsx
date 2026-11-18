import "./globals.css";

import { GeistMono } from "geist/font/mono";
import { GeistSans } from "geist/font/sans";
import type { Metadata, Viewport } from "next";

import { Providers } from "@/components/providers";
import { getServerSession } from "@/lib/auth/session.server";

export const metadata: Metadata = {
  title: {
    default: "Lodge — Find your perfect stay",
    template: "%s · Lodge",
  },
  description:
    "Find and book unique homes, cabins and apartments around the world with Lodge.",
  applicationName: "Lodge",
  keywords: ["accommodation", "booking", "travel", "rentals", "vacation", "lodge"],
  openGraph: {
    title: "Lodge — Find your perfect stay",
    description: "Find and book unique homes, cabins and apartments around the world.",
    type: "website",
    siteName: "Lodge",
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#fcfcfa" },
    { media: "(prefers-color-scheme: dark)", color: "#14161b" },
  ],
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  // Decoding the cookie on the server lets the navbar render the right state
  // on first paint (no flash, no hydration mismatch).
  const session = await getServerSession();

  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${GeistSans.variable} ${GeistMono.variable}`}
    >
      <body className="min-h-dvh font-sans">
        <a
          href="#main"
          className="sr-only z-[100] rounded-lg bg-primary px-4 py-2 text-primary-foreground focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
        >
          Skip to content
        </a>
        <Providers initialSession={session}>{children}</Providers>
      </body>
    </html>
  );
}

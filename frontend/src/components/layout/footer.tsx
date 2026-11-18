import Link from "next/link";
import { useTranslations } from "next-intl";

import { Container } from "@/components/shared/container";
import { Logo } from "@/components/shared/logo";

import { LanguageSwitcher } from "./language-switcher";

const LINKS = [
  {
    title: "explore",
    items: [
      { label: "allStays", href: "/" },
      { label: "search", href: "/search" },
    ],
  },
  {
    title: "yourTrips",
    items: [
      { label: "myBookings", href: "/bookings" },
      { label: "accountSettings", href: "/settings" },
    ],
  },
] as const;

export function Footer() {
  const t = useTranslations("footer");

  return (
    <footer className="mt-24 border-t bg-muted/40">
      <Container className="grid gap-10 py-12 sm:grid-cols-2 md:grid-cols-[2fr_1fr_1fr]">
        <div className="grid content-start gap-3">
          <Logo />
          <p className="max-w-xs text-sm text-muted-foreground">{t("tagline")}</p>
        </div>
        {LINKS.map((group) => (
          <nav key={group.title} aria-label={t(group.title)} className="grid content-start gap-3">
            <h2 className="text-sm font-semibold">{t(group.title)}</h2>
            <ul className="grid gap-2">
              {group.items.map((item) => (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className="text-sm text-muted-foreground transition-colors hover:text-foreground"
                  >
                    {t(item.label)}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </Container>
      <Container className="flex flex-col gap-2 border-t py-6 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
          <LanguageSwitcher variant="compact" />
          <p>{t("copyright", { year: String(new Date().getFullYear()) })}</p>
        </div>
        <p>{t("pricesNote")}</p>
      </Container>
    </footer>
  );
}

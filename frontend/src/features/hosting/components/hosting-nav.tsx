"use client";

import { CalendarRange, Compass, Home, LayoutDashboard, ListChecks, MessageSquareText, Wallet } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

const ITEMS = [
  { href: "/hosting", labelKey: "today", icon: LayoutDashboard, exact: true },
  { href: "/hosting/listings", labelKey: "listings", icon: Home, exact: false },
  { href: "/hosting/reservations", labelKey: "reservations", icon: ListChecks, exact: false },
  { href: "/hosting/experiences", labelKey: "experiences", icon: Compass, exact: false },
  { href: "/hosting/reviews", labelKey: "reviews", icon: MessageSquareText, exact: false },
  { href: "/hosting/payouts", labelKey: "payouts", icon: Wallet, exact: false },
] as const;

/** Sub-navigation shared by every /hosting page. */
export function HostingNav() {
  const t = useTranslations("hosting.nav");
  const pathname = usePathname();
  const calendarMatch = /^\/hosting\/listings\/([^/]+)\/calendar/.exec(pathname);

  return (
    <nav aria-label={t("label")} className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
      {ITEMS.map((item) => {
        const active = item.exact
          ? pathname === item.href
          : pathname === item.href || (pathname.startsWith(`${item.href}/`) && !calendarMatch);
        const Icon = item.icon;
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? "page" : undefined}
            className={cn(
              "inline-flex h-10 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
              active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
            )}
          >
            <Icon className="size-4" aria-hidden="true" />
            {t(item.labelKey)}
          </Link>
        );
      })}
      {calendarMatch && (
        <span
          aria-current="page"
          className="inline-flex h-10 shrink-0 items-center gap-2 rounded-full border border-foreground bg-foreground px-4 text-sm font-medium text-background"
        >
          <CalendarRange className="size-4" aria-hidden="true" />
          {t("calendar")}
        </span>
      )}
    </nav>
  );
}

"use client";

import { Compass, House } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

export function isExperiencesPath(pathname: string) {
  return pathname === "/experiences" || pathname.startsWith("/experiences/");
}

/** Airbnb-style "Stays | Experiences" switch. */
export function DiscoverToggle({ className }: { className?: string }) {
  const t = useTranslations("nav");
  const pathname = usePathname();
  const experiences = isExperiencesPath(pathname);

  const item = (active: boolean) =>
    cn(
      "inline-flex h-9 items-center gap-1.5 rounded-full px-3.5 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
      active ? "bg-background text-foreground shadow-xs" : "text-muted-foreground hover:text-foreground",
    );

  return (
    <nav aria-label={t("discover")} className={cn("inline-flex items-center gap-0.5 rounded-full bg-muted p-1", className)}>
      <Link href="/" aria-current={!experiences ? "page" : undefined} className={item(!experiences)} data-testid="toggle-stays">
        <House className="size-4" aria-hidden="true" />
        {t("stays")}
      </Link>
      <Link
        href="/experiences"
        aria-current={experiences ? "page" : undefined}
        className={item(experiences)}
        data-testid="toggle-experiences"
      >
        <Compass className="size-4" aria-hidden="true" />
        {t("experiences")}
      </Link>
    </nav>
  );
}

const MOBILE_PATHS = ["/", "/search"];

/** The toggle as a strip under the navbar on small screens (discovery pages only). */
export function MobileDiscoverToggle() {
  const pathname = usePathname();
  if (!MOBILE_PATHS.includes(pathname) && !isExperiencesPath(pathname)) return null;
  return (
    <div className="flex justify-center border-b bg-background px-4 py-2 lg:hidden">
      <DiscoverToggle />
    </div>
  );
}

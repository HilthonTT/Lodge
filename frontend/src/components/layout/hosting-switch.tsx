"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { useSession } from "@/features/auth/session-provider";
import { useHostingSummary } from "@/features/hosting/hooks/use-hosting-summary";
import { cn } from "@/lib/utils";

/**
 * Navbar link between travelling and hosting: "Switch to hosting" for hosts,
 * "Lodge your home" for everyone else, "Switch to travelling" inside /hosting.
 */
export function HostingSwitch({ className }: { className?: string }) {
  const t = useTranslations("nav");
  const pathname = usePathname();
  const { session } = useSession();
  const inHosting = pathname === "/hosting" || pathname.startsWith("/hosting/");
  const summary = useHostingSummary({ enabled: Boolean(session) && !inHosting });
  const isHost = (summary.data?.listingCount ?? 0) > 0;

  return (
    <Button asChild variant="ghost" className={cn("rounded-full font-medium", className)}>
      <Link href={inHosting ? "/" : "/hosting"}>
        {inHosting ? t("switchToTravelling") : isHost ? t("switchToHosting") : t("lodgeYourHome")}
      </Link>
    </Button>
  );
}

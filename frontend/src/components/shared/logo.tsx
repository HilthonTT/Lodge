import { TentTree } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

export function Logo({ className, compact = false }: { className?: string; compact?: boolean }) {
  const t = useTranslations("common");
  return (
    <Link
      href="/"
      aria-label={t("logoHome")}
      className={cn(
        "group inline-flex items-center gap-2 rounded-lg font-semibold tracking-tight text-foreground",
        className,
      )}
    >
      <span className="grid size-9 place-items-center rounded-xl bg-primary text-primary-foreground shadow-xs transition-transform group-hover:-rotate-6">
        <TentTree className="size-5" aria-hidden="true" />
      </span>
      <span className={cn("text-lg", compact && "sr-only sm:not-sr-only")}>Lodge</span>
    </Link>
  );
}

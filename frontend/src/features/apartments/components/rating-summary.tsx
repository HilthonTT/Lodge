import { Star } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { formatNumber, formatRating } from "@/lib/format";
import { cn } from "@/lib/utils";

type RatingSummaryProps = {
  averageRating: number | null;
  reviewCount: number;
  /** "compact" → `★ 4.8 (12)`; "full" → `★ 4.8 · 12 reviews`. */
  variant?: "compact" | "full";
  className?: string;
};

/** Average rating as returned by the API, or a "New" label for places without reviews. */
export function RatingSummary({ averageRating, reviewCount, variant = "compact", className }: RatingSummaryProps) {
  const t = useTranslations();
  const locale = useLocale();

  if (averageRating === null || reviewCount === 0) {
    return (
      <span className={cn("inline-flex items-center gap-1 text-sm font-medium", className)}>
        <Star className="size-3.5 fill-current" aria-hidden="true" />
        <span aria-hidden="true">{t("apartment.rating.new")}</span>
        <span className="sr-only">{t("apartment.rating.newLabel")}</span>
      </span>
    );
  }

  const value = formatRating(averageRating, locale);
  return (
    <span
      className={cn("inline-flex items-center gap-1 text-sm whitespace-nowrap", className)}
      aria-label={t("apartment.rating.rated", { rating: value, count: reviewCount })}
    >
      <Star className="size-3.5 fill-star text-star" aria-hidden="true" />
      <span className="font-semibold tabular-nums">{value}</span>
      {variant === "compact" ? (
        <span className="text-muted-foreground tabular-nums">({formatNumber(reviewCount, locale)})</span>
      ) : (
        <span className="text-muted-foreground">· {t("common.reviews", { count: reviewCount })}</span>
      )}
    </span>
  );
}

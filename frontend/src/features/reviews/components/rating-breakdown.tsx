import { useLocale, useTranslations } from "next-intl";

import { formatRating } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type CategoryRatings, REVIEW_CATEGORIES } from "@/types/api";

/** Airbnb-style per-category averages: label, bar and value. */
export function RatingBreakdown({ breakdown, className }: { breakdown: CategoryRatings; className?: string }) {
  const t = useTranslations("reviews");
  const locale = useLocale();
  return (
    <dl className={cn("grid gap-x-12 gap-y-3 sm:grid-cols-2", className)} data-testid="rating-breakdown">
      {REVIEW_CATEGORIES.map((category) => {
        const value = breakdown[category] ?? 0;
        const label = t(`categories.${category}`);
        return (
          <div key={category} className="grid grid-cols-[minmax(0,1fr)_6rem_2rem] items-center gap-3 text-sm">
            <dt>{label}</dt>
            <dd className="contents">
              <span
                role="meter"
                aria-label={label}
                aria-valuemin={0}
                aria-valuemax={5}
                aria-valuenow={value}
                aria-valuetext={t("outOfFive", { value: formatRating(value, locale) })}
                className="h-1 overflow-hidden rounded-full bg-muted"
              >
                <span className="block h-full rounded-full bg-foreground" style={{ width: `${(value / 5) * 100}%` }} />
              </span>
              <span className="text-right font-semibold tabular-nums" aria-hidden="true">
                {formatRating(value, locale)}
              </span>
            </dd>
          </div>
        );
      })}
    </dl>
  );
}

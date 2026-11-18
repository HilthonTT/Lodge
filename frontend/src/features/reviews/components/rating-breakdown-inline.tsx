import { Star } from "lucide-react";
import { useTranslations } from "next-intl";

import { type CategoryRatings, REVIEW_CATEGORIES } from "@/types/api";

/** One review's category ratings as compact chips: "Cleanliness ★ 4". */
export function RatingBreakdownInline({ categories }: { categories: CategoryRatings | null | undefined }) {
  const t = useTranslations("reviews");
  if (!categories) return null;
  return (
    <ul className="flex flex-wrap gap-1.5" aria-label={t("categoryRatings")}>
      {REVIEW_CATEGORIES.map((category) => (
        <li
          key={category}
          className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs text-muted-foreground"
        >
          {t(`categories.${category}`)}
          <Star className="size-3 fill-star text-star" aria-hidden="true" />
          <span className="font-semibold text-foreground tabular-nums">{categories[category]}</span>
        </li>
      ))}
    </ul>
  );
}

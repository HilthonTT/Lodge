import { Star } from "lucide-react";

import { pluralize } from "@/lib/format";
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
  if (averageRating === null || reviewCount === 0) {
    return (
      <span className={cn("inline-flex items-center gap-1 text-sm font-medium", className)}>
        <Star className="size-3.5 fill-current" aria-hidden="true" />
        New
        <span className="sr-only"> listing, no reviews yet</span>
      </span>
    );
  }

  const value = averageRating.toFixed(1);
  return (
    <span
      className={cn("inline-flex items-center gap-1 text-sm whitespace-nowrap", className)}
      aria-label={`Rated ${value} out of 5 from ${pluralize(reviewCount, "review")}`}
    >
      <Star className="size-3.5 fill-star text-star" aria-hidden="true" />
      <span className="font-semibold tabular-nums">{value}</span>
      {variant === "compact" ? (
        <span className="text-muted-foreground tabular-nums">({reviewCount.toLocaleString("en-US")})</span>
      ) : (
        <span className="text-muted-foreground">· {pluralize(reviewCount, "review")}</span>
      )}
    </span>
  );
}

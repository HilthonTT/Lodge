import { MessageSquareText, Star } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { formatMonthYear } from "@/lib/dates";
import { cn, initials } from "@/lib/utils";
import type { PagedList, Review } from "@/types/api";

import { RatingSummary } from "./rating-summary";

export function StarRating({ value, className }: { value: number; className?: string }) {
  const t = useTranslations("apartment.reviews");
  return (
    <span className={cn("inline-flex items-center gap-0.5", className)} aria-label={t("stars", { value })}>
      {Array.from({ length: 5 }, (_, i) => (
        <Star
          key={i}
          aria-hidden="true"
          className={cn("size-3.5", i < Math.round(value) ? "fill-star text-star" : "text-muted-foreground/40")}
        />
      ))}
    </span>
  );
}

type ReviewsSectionProps = {
  reviews: PagedList<Review> | null;
  /** Aggregates from the apartment itself, so they cover every review, not just this page. */
  averageRating: number | null;
  reviewCount: number;
};

export function ReviewsSection({ reviews, averageRating, reviewCount }: ReviewsSectionProps) {
  const t = useTranslations();
  const locale = useLocale();
  const items = reviews?.items ?? [];

  return (
    <section aria-labelledby="reviews-heading" className="grid scroll-mt-28 gap-6">
      <div className="flex flex-wrap items-center gap-3">
        <h2 id="reviews-heading" className="scroll-mt-28 text-xl font-semibold">
          {t("apartment.reviews.heading")}
        </h2>
        {reviewCount > 0 && (
          <RatingSummary averageRating={averageRating} reviewCount={reviewCount} variant="full" />
        )}
      </div>

      {reviews === null ? (
        <p className="text-sm text-muted-foreground">{t("apartment.reviews.unavailable")}</p>
      ) : items.length === 0 ? (
        <div className="flex items-center gap-3 rounded-2xl border border-dashed p-5 text-sm text-muted-foreground">
          <MessageSquareText className="size-5 shrink-0" aria-hidden="true" />
          {t("apartment.reviews.empty")}
        </div>
      ) : (
        <ul className="grid gap-4 sm:grid-cols-2">
          {items.map((review) => (
            <li key={review.id} className="grid content-start gap-3 rounded-2xl border bg-card p-5">
              <div className="flex items-center gap-3">
                <Avatar className="size-10">
                  <AvatarFallback className="bg-accent text-sm text-accent-foreground">
                    {initials(review.authorName)}
                  </AvatarFallback>
                </Avatar>
                <div className="grid min-w-0 gap-0.5">
                  <Link
                    href={`/users/${review.userId}`}
                    className="truncate text-sm font-semibold underline-offset-4 hover:underline focus-visible:underline"
                  >
                    {review.authorName || t("common.guestFallback")}
                  </Link>
                  <time dateTime={review.createdOnUtc} className="text-xs text-muted-foreground">
                    {formatMonthYear(new Date(review.createdOnUtc), locale)}
                  </time>
                </div>
              </div>
              <StarRating value={review.rating} />
              <p className="text-sm leading-relaxed whitespace-pre-line">{review.comment}</p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

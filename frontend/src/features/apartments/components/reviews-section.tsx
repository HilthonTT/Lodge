import { MessageSquareText } from "lucide-react";
import { useTranslations } from "next-intl";

import { RatingBreakdown } from "@/features/reviews/components/rating-breakdown";
import { ReviewItem } from "@/features/reviews/components/review-item";
import type { CategoryRatings, PagedList, Review } from "@/types/api";

import { RatingSummary } from "./rating-summary";

type ReviewsSectionProps = {
  reviews: PagedList<Review> | null;
  /** Aggregates from the apartment itself, so they cover every review, not just this page. */
  averageRating: number | null;
  reviewCount: number;
  ratingBreakdown?: CategoryRatings | null;
  hostName?: string;
};

export function ReviewsSection({ reviews, averageRating, reviewCount, ratingBreakdown, hostName }: ReviewsSectionProps) {
  const t = useTranslations();
  // The signed-in reviewer's own unpublished review comes first.
  const items = [...(reviews?.items ?? [])].sort((a, b) => Number(a.isPublished) - Number(b.isPublished));

  return (
    <section id="reviews" aria-labelledby="reviews-heading" className="grid scroll-mt-28 gap-6">
      <div className="flex flex-wrap items-center gap-3">
        <h2 id="reviews-heading" className="scroll-mt-28 text-xl font-semibold">
          {t("apartment.reviews.heading")}
        </h2>
        {reviewCount > 0 && (
          <RatingSummary averageRating={averageRating} reviewCount={reviewCount} variant="full" />
        )}
      </div>

      {ratingBreakdown && reviewCount > 0 && <RatingBreakdown breakdown={ratingBreakdown} />}

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
            <li key={review.id} className="grid">
              <ReviewItem review={review} hostName={hostName} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

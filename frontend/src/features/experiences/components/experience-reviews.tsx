"use client";

import { Star } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { formatMonthYear } from "@/lib/dates";
import type { ExperienceReview, PagedList } from "@/types/api";

import { useExperienceReviews } from "../hooks/use-experiences";

export function ExperienceReviews({
  experienceId,
  averageRating,
  reviewCount,
  initial,
}: {
  experienceId: string;
  averageRating: number | null;
  reviewCount: number;
  initial: PagedList<ExperienceReview> | null;
}) {
  const t = useTranslations("experiences.detail");
  const locale = useLocale();
  const [page, setPage] = useState(1);
  const reviews = useExperienceReviews(experienceId, page, initial);
  const [shown, setShown] = useState<ExperienceReview[]>(initial?.items ?? []);
  const [seen, setSeen] = useState(reviews.data);
  // Accumulate pages as "Show more" loads them.
  if (reviews.data && reviews.data !== seen && !reviews.isPlaceholderData) {
    setSeen(reviews.data);
    const ids = new Set(shown.map((r) => r.id));
    setShown([...shown, ...reviews.data.items.filter((r) => !ids.has(r.id))]);
  }

  return (
    <section aria-labelledby="reviews-heading" className="grid gap-6">
      <h2 id="reviews-heading" className="flex items-center gap-2 text-xl font-semibold">
        <RatingSummary averageRating={averageRating} reviewCount={reviewCount} variant="full" className="text-xl" />
      </h2>
      {shown.length === 0 ? (
        <p className="text-muted-foreground">{t("noReviews")}</p>
      ) : (
        <ul className="grid gap-8 md:grid-cols-2">
          {shown.map((review) => (
            <li key={review.id} className="grid content-start gap-3">
              <Link href={`/users/${review.guest.id}`} className="flex items-center gap-3">
                <UserAvatar name={review.guest.firstName} src={review.guest.avatarUrl} className="size-11" />
                <span className="grid">
                  <span className="font-semibold">{review.guest.firstName}</span>
                  <span className="text-sm text-muted-foreground">{formatMonthYear(new Date(review.createdOnUtc), locale)}</span>
                </span>
              </Link>
              <span className="flex gap-0.5" aria-label={t("stars", { count: review.rating })}>
                {Array.from({ length: 5 }, (_, i) => (
                  <Star
                    key={i}
                    className={i < review.rating ? "size-3.5 fill-star text-star" : "size-3.5 text-muted-foreground/40"}
                    aria-hidden="true"
                  />
                ))}
              </span>
              <p className="leading-relaxed whitespace-pre-line">{review.comment}</p>
            </li>
          ))}
        </ul>
      )}
      {reviews.data?.hasNextPage && (
        <Button variant="outline" className="justify-self-start" onClick={() => setPage((p) => p + 1)} disabled={reviews.isFetching}>
          {t("moreReviews")}
        </Button>
      )}
    </section>
  );
}

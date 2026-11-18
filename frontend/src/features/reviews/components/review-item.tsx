import { CornerDownRight, EyeOff, Star } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Badge } from "@/components/ui/badge";
import { formatMonthYear } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Review } from "@/types/api";

export function StarRating({ value, className }: { value: number; className?: string }) {
  const t = useTranslations("apartment.reviews");
  return (
    <span className={cn("inline-flex items-center gap-0.5", className)} role="img" aria-label={t("stars", { value })}>
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

type ReviewItemProps = {
  review: Review;
  /** Name shown on the host's reply ("Response from Sam"). */
  hostName?: string;
  /** Extra content under the review, e.g. the host's reply editor. */
  children?: React.ReactNode;
  className?: string;
};

/**
 * A guest's review: author with avatar, stars, comment, and the host's reply
 * indented underneath. The reviewer's own unpublished review is marked
 * "Only visible to you".
 */
export function ReviewItem({ review, hostName, children, className }: ReviewItemProps) {
  const t = useTranslations("reviews");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const author = review.authorName || tCommon("guestFallback");

  return (
    <article
      className={cn(
        "grid content-start gap-3 rounded-2xl border bg-card p-5",
        !review.isPublished && "border-dashed bg-muted/30",
        className,
      )}
      data-review-id={review.id}
    >
      <div className="flex items-center gap-3">
        <UserAvatar name={author} src={review.authorAvatarUrl} className="size-10" fallbackClassName="bg-accent text-sm text-accent-foreground" />
        <div className="grid min-w-0 gap-0.5">
          <Link
            href={`/users/${review.userId}`}
            className="truncate text-sm font-semibold underline-offset-4 hover:underline focus-visible:underline"
          >
            {author}
          </Link>
          <time dateTime={review.createdOnUtc} className="text-xs text-muted-foreground">
            {formatMonthYear(new Date(review.createdOnUtc), locale)}
          </time>
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <StarRating value={review.rating} />
        {!review.isPublished && (
          <Badge variant="muted" data-testid="own-unpublished">
            <EyeOff aria-hidden="true" /> {t("onlyVisibleToYou")}
          </Badge>
        )}
      </div>
      <p className="text-sm leading-relaxed whitespace-pre-line">{review.comment}</p>
      {!review.isPublished && <p className="text-xs text-muted-foreground">{t("unpublishedHint")}</p>}
      {review.hostReply && (
        <div className="ml-4 grid gap-1 border-l-2 pl-4 text-sm" data-testid="host-reply">
          <p className="flex items-center gap-1.5 font-medium">
            <CornerDownRight className="size-4 text-muted-foreground" aria-hidden="true" />
            {hostName ? t("responseFrom", { name: hostName }) : t("responseFromHost")}
            <time dateTime={review.hostReply.createdOnUtc} className="text-xs font-normal text-muted-foreground">
              · {formatMonthYear(new Date(review.hostReply.modifiedOnUtc ?? review.hostReply.createdOnUtc), locale)}
            </time>
          </p>
          <p className="leading-relaxed whitespace-pre-line text-muted-foreground">{review.hostReply.text}</p>
        </div>
      )}
      {children}
    </article>
  );
}

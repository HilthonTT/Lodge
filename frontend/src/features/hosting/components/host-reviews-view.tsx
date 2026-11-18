"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, MessageSquareReply, MessageSquareText, Pencil, Trash2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState } from "@/components/shared/state";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";
import { useSession } from "@/features/auth/session-provider";
import { RatingBreakdownInline } from "@/features/reviews/components/rating-breakdown-inline";
import { ReviewItem } from "@/features/reviews/components/review-item";
import { useHostReviews, useReplyToReview } from "@/features/reviews/hooks/use-reviews";
import { createReplySchema, REPLY_MAX, type ReplyFormValues } from "@/features/reviews/schemas";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";
import type { Review } from "@/types/api";

import { useHostListings } from "../hooks/use-hosting";
import { HOST_REVIEWS_PAGE_SIZE, type HostReviewsSearch, hostReviewsHref } from "../host-reviews-search-params";
import { BookingsSkeleton } from "./hosting-dashboard";
import { HostingError } from "./hosting-gate";

/** /hosting/reviews: published reviews of the host's listings, with replies. */
export function HostReviewsView({ search }: { search: HostReviewsSearch }) {
  const t = useTranslations("hosting.reviews");
  const router = useRouter();
  const { session } = useSession();
  const hostName = session?.name.split(" ")[0];
  const listings = useHostListings({ page: 1, pageSize: 100 });
  const reviews = useHostReviews({ page: search.page, pageSize: HOST_REVIEWS_PAGE_SIZE, listingId: search.listing });
  const pages = reviews.data ? Math.ceil(reviews.data.totalCount / HOST_REVIEWS_PAGE_SIZE) : 0;

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground" aria-live="polite">
          {reviews.data ? t("count", { count: reviews.data.totalCount }) : " "}
        </p>
        <div>
          <label htmlFor="reviews-listing-filter" className="sr-only">
            {t("listingFilter")}
          </label>
          <select
            id="reviews-listing-filter"
            value={search.listing ?? ""}
            onChange={(e) =>
              router.push(hostReviewsHref({ listing: e.target.value || undefined, page: 1 }), { scroll: false })
            }
            className="h-10 w-full max-w-xs appearance-none rounded-full border border-input bg-background px-4 text-sm font-medium shadow-xs outline-none focus-visible:ring-[3px] focus-visible:ring-ring/30 dark:bg-input/20"
          >
            <option value="">{t("allListings")}</option>
            {listings.data?.items.map((listing) => (
              <option key={listing.id} value={listing.id}>
                {listing.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {reviews.isPending ? (
        <BookingsSkeleton count={3} />
      ) : reviews.isError ? (
        <HostingError error={reviews.error} title={t("loadError")} onRetry={() => reviews.refetch()} />
      ) : reviews.data.items.length === 0 ? (
        <EmptyState icon={MessageSquareText} title={t("emptyTitle")} description={t("emptyDescription")} />
      ) : (
        <>
          <ul className={cn("grid gap-4", reviews.isPlaceholderData && "opacity-60 transition-opacity")}>
            {reviews.data.items.map((review) => (
              <li key={review.id}>
                <ReviewItem review={review} hostName={hostName}>
                  <p className="text-xs text-muted-foreground">
                    <Link href={`/apartments/${review.apartmentId}`} className="font-medium underline-offset-4 hover:underline">
                      {review.listingName}
                    </Link>
                  </p>
                  <RatingBreakdownInline categories={review.categories} />
                  <ReplyControls review={review} />
                </ReviewItem>
              </li>
            ))}
          </ul>
          <Pagination
            page={search.page}
            totalPages={pages}
            pathname="/hosting/reviews"
            searchParams={{ listing: search.listing }}
          />
        </>
      )}
    </div>
  );
}

function ReplyControls({ review }: { review: Review }) {
  const t = useTranslations("hosting.reviews");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const [editing, setEditing] = useState(false);
  const remove = useReplyToReview();

  if (editing) return <ReplyForm review={review} onDone={() => setEditing(false)} />;

  if (!review.hostReply) {
    return (
      <Button variant="outline" size="sm" className="justify-self-start" onClick={() => setEditing(true)}>
        <MessageSquareReply aria-hidden="true" /> {t("reply")}
      </Button>
    );
  }

  const onDelete = () =>
    remove.mutate(
      { reviewId: review.id, text: null },
      {
        onSuccess: () => toast.success(t("replyDeleted")),
        onError: (error) => toast.error(t("replyError"), { description: errorMessage(error) }),
      },
    );

  return (
    <div className="flex flex-wrap gap-2">
      <Button variant="ghost" size="sm" onClick={() => setEditing(true)}>
        <Pencil aria-hidden="true" /> {t("editReply")}
      </Button>
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" disabled={remove.isPending}>
            {remove.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Trash2 aria-hidden="true" />}
            {t("deleteReply")}
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("deleteTitle")}</AlertDialogTitle>
            <AlertDialogDescription>{t("deleteDescription", { name: review.authorName })}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={onDelete}>
              {tCommon("delete")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ReplyForm({ review, onDone }: { review: Review; onDone: () => void }) {
  const t = useTranslations("hosting.reviews");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const reply = useReplyToReview();
  const schema = useMemo(() => createReplySchema(tValidation), [tValidation]);
  const form = useForm<ReplyFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { text: review.hostReply?.text ?? "" },
  });
  const text = useWatch({ control: form.control, name: "text" }) ?? "";
  const fieldId = `reply-${review.id}`;

  const onSubmit = form.handleSubmit(async ({ text: value }) => {
    try {
      await reply.mutateAsync({ reviewId: review.id, text: value });
      toast.success(review.hostReply ? t("replyUpdated") : t("replyPosted"));
      onDone();
    } catch {
      // shown inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="ml-4 grid gap-3 border-l-2 pl-4">
      <Field
        id={fieldId}
        label={review.hostReply ? t("editReply") : t("yourReply")}
        error={form.formState.errors.text?.message ?? errorMessage(reply.error)}
        description={t("replyHint")}
        aside={
          <span className={cn("text-xs text-muted-foreground tabular-nums", text.length > REPLY_MAX && "text-destructive")}>
            {text.length}/{REPLY_MAX}
          </span>
        }
      >
        {(control) => <Textarea {...control} {...form.register("text")} rows={3} autoFocus placeholder={t("replyPlaceholder")} />}
      </Field>
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={reply.isPending}>
          {reply.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {review.hostReply ? tCommon("save") : t("postReply")}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={onDone}>
          {tCommon("cancel")}
        </Button>
      </div>
    </form>
  );
}

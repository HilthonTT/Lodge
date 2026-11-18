"use client";

import { Loader2, Star } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/features/auth/components/form-error";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";
import { uuid } from "@/lib/uuid";

import { useCreateReview } from "../hooks/use-bookings";

const MAX_COMMENT = 500;
const RATINGS = [1, 2, 3, 4, 5] as const;

type ReviewDialogProps = {
  bookingId: string;
  apartmentName: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export function ReviewDialog({ bookingId, apartmentName, open, onOpenChange }: ReviewDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        {open && (
          <ReviewForm bookingId={bookingId} apartmentName={apartmentName} onDone={() => onOpenChange(false)} />
        )}
      </DialogContent>
    </Dialog>
  );
}

function ReviewForm({
  bookingId,
  apartmentName,
  onDone,
}: {
  bookingId: string;
  apartmentName: string;
  onDone: () => void;
}) {
  const t = useTranslations();
  const errorMessage = useApiErrorMessage();
  const [rating, setRating] = useState(0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState("");
  const [touched, setTouched] = useState(false);
  // Stable per dialog session so a retried submit isn't recorded twice.
  const [idempotencyKey] = useState(uuid);
  const createReview = useCreateReview();

  const commentError = !comment.trim()
    ? t("validation.commentRequired")
    : comment.length > MAX_COMMENT
      ? t("validation.commentTooLong", { max: MAX_COMMENT })
      : undefined;
  const ratingError = rating === 0 ? t("validation.ratingRequired") : undefined;

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (commentError || ratingError) return;
    try {
      await createReview.mutateAsync({ bookingId, rating, comment: comment.trim(), idempotencyKey });
      toast.success(t("bookings.review.success"));
      onDone();
    } catch {
      // shown inline
    }
  };

  const shown = hover || rating;
  const labelFor = (value: number) => t(`bookings.review.labels.${String(value) as "1" | "2" | "3" | "4" | "5"}`);

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <DialogHeader>
        <DialogTitle>{t("bookings.review.title")}</DialogTitle>
        <DialogDescription>{t("bookings.review.description", { name: apartmentName })}</DialogDescription>
      </DialogHeader>

      <FormError message={errorMessage(createReview.error)} />

      <fieldset className="grid gap-2">
        <legend className="mb-2 text-sm font-medium">{t("bookings.review.rating")}</legend>
        <div className="flex items-center gap-3">
          <div className="flex" role="radiogroup" aria-label={t("bookings.review.rating")} onMouseLeave={() => setHover(0)}>
            {RATINGS.map((value) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={rating === value}
                aria-label={t("bookings.review.starLabel", { count: value, label: labelFor(value) })}
                onClick={() => setRating(value)}
                onMouseEnter={() => setHover(value)}
                className="rounded-md p-1 transition-transform hover:scale-110 focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
              >
                <Star
                  className={cn("size-7", value <= shown ? "fill-star text-star" : "text-muted-foreground/40")}
                  aria-hidden="true"
                />
              </button>
            ))}
          </div>
          <span className="text-sm text-muted-foreground">{shown ? labelFor(shown) : ""}</span>
        </div>
        {touched && ratingError && <p className="text-xs font-medium text-destructive">{ratingError}</p>}
      </fieldset>

      <Field
        id="review-comment"
        label={t("bookings.review.yourReview")}
        error={touched ? commentError : undefined}
        aside={
          <span className={cn("text-xs text-muted-foreground tabular-nums", comment.length > MAX_COMMENT && "text-destructive")}>
            {comment.length}/{MAX_COMMENT}
          </span>
        }
      >
        {(control) => (
          <Textarea
            {...control}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            rows={5}
            placeholder={t("bookings.review.placeholder")}
          />
        )}
      </Field>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          {t("common.cancel")}
        </Button>
        <Button type="submit" disabled={createReview.isPending}>
          {createReview.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {t("bookings.review.submit")}
        </Button>
      </DialogFooter>
    </form>
  );
}

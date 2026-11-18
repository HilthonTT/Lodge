"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Star } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
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
import { StarInput } from "@/features/reviews/components/star-input";
import { useReviewGuest } from "@/features/reviews/hooks/use-reviews";
import { createGuestReviewSchema, type GuestReviewFormValues, REVIEW_COMMENT_MAX } from "@/features/reviews/schemas";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatMediumDate, parseDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Booking } from "@/types/api";

/** "Review guest" button + dialog for a completed stay the host may still review. */
export function ReviewGuestButton({ booking }: { booking: Booking }) {
  const t = useTranslations("hosting.guestReview");
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button size="sm" variant="outline" onClick={() => setOpen(true)}>
        <Star aria-hidden="true" /> {t("button")}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-lg">
          {open && <GuestReviewForm booking={booking} onDone={() => setOpen(false)} />}
        </DialogContent>
      </Dialog>
    </>
  );
}

function GuestReviewForm({ booking, onDone }: { booking: Booking; onDone: () => void }) {
  const t = useTranslations("hosting.guestReview");
  const tCommon = useTranslations("common");
  const tReviews = useTranslations("reviews");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const review = useReviewGuest();
  const guest = booking.guestName || booking.guestEmail;
  const deadline = parseDateOnly(booking.reviewDeadline);

  const schema = useMemo(() => createGuestReviewSchema(tValidation), [tValidation]);
  const form = useForm<GuestReviewFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { rating: 0, comment: "" },
  });
  const { errors } = form.formState;
  const comment = useWatch({ control: form.control, name: "comment" }) ?? "";

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await review.mutateAsync({ bookingId: booking.id, ...values });
      toast.success(t("success"), { description: t("successDescription", { guest }) });
      onDone();
    } catch {
      // shown inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <DialogHeader>
        <DialogTitle>{t("title", { guest })}</DialogTitle>
        <DialogDescription>{t("description", { listing: booking.apartmentName })}</DialogDescription>
      </DialogHeader>
      <FormError message={errorMessage(review.error)} />
      <Controller
        control={form.control}
        name="rating"
        render={({ field }) => (
          <StarInput
            label={t("rating")}
            value={field.value}
            onChange={field.onChange}
            size="lg"
            showLabel
            error={errors.rating?.message}
          />
        )}
      />
      <Field
        id="guest-review-comment"
        label={t("comment")}
        error={errors.comment?.message}
        aside={
          <span className={cn("text-xs text-muted-foreground tabular-nums", comment.length > REVIEW_COMMENT_MAX && "text-destructive")}>
            {comment.length}/{REVIEW_COMMENT_MAX}
          </span>
        }
      >
        {(control) => <Textarea {...control} {...form.register("comment")} rows={4} placeholder={t("placeholder")} />}
      </Field>
      <p className="text-xs text-muted-foreground">
        {deadline ? tReviews("hostDoubleBlind", { date: formatMediumDate(deadline, locale) }) : tReviews("doubleBlindHint")}
      </p>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          {tCommon("cancel")}
        </Button>
        <Button type="submit" disabled={review.isPending}>
          {review.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {t("submit")}
        </Button>
      </DialogFooter>
    </form>
  );
}

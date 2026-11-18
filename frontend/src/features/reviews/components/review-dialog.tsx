"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { CalendarClock, EyeOff, Loader2, PartyPopper } from "lucide-react";
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
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/features/auth/components/form-error";
import { useIdempotencyKey } from "@/hooks/use-idempotency-key";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatMediumDate, parseDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import { REVIEW_CATEGORIES, type Review } from "@/types/api";

import { useCreateReview, useUpdateReview } from "../hooks/use-reviews";
import { createReviewSchema, REVIEW_COMMENT_MAX, type ReviewFormValues } from "../schemas";
import { StarInput } from "./star-input";

type ReviewDialogProps = {
  bookingId: string;
  apartmentName: string;
  /** yyyy-MM-dd: last day to review; the review is published by then at the latest. */
  reviewDeadline: string | null;
  /** The guest's own unpublished review, to edit it. */
  existing?: Review | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/** Write (or, while unpublished, edit) a review of a completed stay. */
export function ReviewDialog({ open, onOpenChange, ...props }: ReviewDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-xl">
        {open && <ReviewForm {...props} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

const EMPTY: ReviewFormValues = {
  rating: 0,
  comment: "",
  categories: { cleanliness: 0, accuracy: 0, checkIn: 0, communication: 0, location: 0, value: 0 },
};

function ReviewForm({
  bookingId,
  apartmentName,
  reviewDeadline,
  existing,
  onDone,
}: Omit<ReviewDialogProps, "open" | "onOpenChange"> & { onDone: () => void }) {
  const t = useTranslations("reviews");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  // One key per distinct review: a retried submit isn't recorded twice, an edited one gets a new key.
  const idempotencyKeyFor = useIdempotencyKey();
  const [submitted, setSubmitted] = useState(false);
  const create = useCreateReview();
  const update = useUpdateReview();
  const mutation = existing ? update : create;

  const schema = useMemo(() => createReviewSchema(tValidation), [tValidation]);
  const form = useForm<ReviewFormValues>({
    resolver: zodResolver(schema),
    defaultValues: existing
      ? { rating: existing.rating, comment: existing.comment, categories: { ...existing.categories } }
      : EMPTY,
  });
  const { errors } = form.formState;
  const comment = useWatch({ control: form.control, name: "comment" }) ?? "";
  const deadline = parseDateOnly(reviewDeadline);
  const deadlineText = deadline ? formatMediumDate(deadline, locale) : null;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (existing) {
        await update.mutateAsync({ reviewId: existing.id, ...values });
        toast.success(t("updated"));
        onDone();
      } else {
        await create.mutateAsync({ bookingId, idempotencyKey: idempotencyKeyFor({ bookingId, ...values }), ...values });
        setSubmitted(true);
      }
    } catch {
      // shown inline
    }
  });

  if (submitted) {
    return (
      <div className="grid gap-5" role="status">
        <span className="grid size-12 place-items-center rounded-2xl bg-accent text-accent-foreground">
          <PartyPopper className="size-6" aria-hidden="true" />
        </span>
        <DialogHeader>
          <DialogTitle>{t("thanksTitle")}</DialogTitle>
          <DialogDescription>
            {deadlineText ? t("doubleBlind", { date: deadlineText }) : t("doubleBlindNoDate")}
          </DialogDescription>
        </DialogHeader>
        <p className="flex items-start gap-2 rounded-xl bg-muted/60 p-3 text-sm text-muted-foreground">
          <EyeOff className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          {t("editWhileUnpublished")}
        </p>
        <DialogFooter>
          <Button onClick={onDone}>{tCommon("close")}</Button>
        </DialogFooter>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <DialogHeader>
        <DialogTitle>{existing ? t("editTitle") : t("title")}</DialogTitle>
        <DialogDescription>{t("description", { name: apartmentName })}</DialogDescription>
      </DialogHeader>

      {deadlineText && (
        <p className="flex items-center gap-2 text-sm text-muted-foreground">
          <CalendarClock className="size-4 shrink-0" aria-hidden="true" />
          {t("deadline", { date: deadlineText })}
        </p>
      )}

      <FormError message={errorMessage(mutation.error)} />

      <Controller
        control={form.control}
        name="rating"
        render={({ field }) => (
          <StarInput
            label={t("overall")}
            value={field.value}
            onChange={field.onChange}
            size="lg"
            showLabel
            error={errors.rating?.message}
          />
        )}
      />

      <Separator />

      <div className="grid gap-3">
        <p className="text-sm text-muted-foreground">{t("categoriesIntro")}</p>
        {REVIEW_CATEGORIES.map((category) => (
          <Controller
            key={category}
            control={form.control}
            name={`categories.${category}`}
            render={({ field }) => (
              <StarInput
                label={t(`categories.${category}`)}
                value={field.value}
                onChange={field.onChange}
                error={errors.categories?.[category]?.message}
              />
            )}
          />
        ))}
      </div>

      <Separator />

      <Field
        id="review-comment"
        label={t("yourReview")}
        error={errors.comment?.message}
        aside={
          <span
            className={cn(
              "text-xs text-muted-foreground tabular-nums",
              comment.length > REVIEW_COMMENT_MAX && "text-destructive",
            )}
          >
            {comment.length}/{REVIEW_COMMENT_MAX}
          </span>
        }
      >
        {(control) => <Textarea {...control} {...form.register("comment")} rows={5} placeholder={t("placeholder")} />}
      </Field>

      <p className="flex items-start gap-2 text-xs text-muted-foreground">
        <EyeOff className="mt-0.5 size-3.5 shrink-0" aria-hidden="true" />
        {t("doubleBlindHint")}
      </p>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          {tCommon("cancel")}
        </Button>
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {existing ? t("saveChanges") : t("submit")}
        </Button>
      </DialogFooter>
    </form>
  );
}

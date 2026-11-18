"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { CircleCheck, CreditCard, Loader2, Star, XCircle } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

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
import type { ValidationTranslator } from "@/features/auth/schemas";
import { useNowMinute } from "@/features/hosting/hooks/use-now";
import { StarInput } from "@/features/reviews/components/star-input";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatSessionDateTime } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import type { ExperienceBooking } from "@/types/api";

import { useCancelExperienceBooking, useExperienceCheckout, useReviewExperience } from "../hooks/use-experiences";

const COMMENT_MAX = 500;

/** Pay, cancel (with the 24-hour rule explained) and review buttons for a guest's experience booking. */
export function ExperienceTripActions({ booking }: { booking: ExperienceBooking }) {
  const t = useTranslations("experiences.trips");
  const errorMessage = useApiErrorMessage();
  const checkout = useExperienceCheckout();

  return (
    <>
      {booking.status === "Reserved" && (
        <Button
          onClick={() =>
            checkout.mutate(booking.id, {
              onError: (error) => toast.error(t("payError"), { description: errorMessage(error) }),
            })
          }
          disabled={checkout.isPending || checkout.isSuccess}
        >
          {checkout.isPending || checkout.isSuccess ? (
            <Loader2 className="animate-spin" aria-hidden="true" />
          ) : (
            <CreditCard aria-hidden="true" />
          )}
          {t("pay")}
        </Button>
      )}
      {booking.canCancel && <CancelExperienceButton booking={booking} />}
      {booking.status === "Completed" &&
        (booking.hasReview ? (
          <p className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
            <CircleCheck className="size-4 text-success" aria-hidden="true" /> {t("reviewed")}
          </p>
        ) : (
          booking.canReview && <ReviewExperienceButton booking={booking} />
        ))}
    </>
  );
}

function CancelExperienceButton({ booking }: { booking: ExperienceBooking }) {
  const t = useTranslations("experiences.trips.cancel");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const cancel = useCancelExperienceBooking();
  const now = useNowMinute();
  // Before hydration (now = 0) assume the free window is still open; the API decides anyway.
  const free = new Date(booking.freeCancellationUntilUtc).getTime() > now;
  const paid = booking.amountPaid !== null && booking.amountPaid > 0;
  const amount = formatPrice(booking.amountPaid ?? booking.totalPrice, booking.currency, locale);
  const deadline = formatSessionDateTime(new Date(booking.freeCancellationUntilUtc), locale);

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="outline" data-testid="experience-cancel">
          <XCircle aria-hidden="true" /> {t("button")}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("title")}</AlertDialogTitle>
          <AlertDialogDescription asChild>
            <div className="grid gap-3">
              <p>{t("rule")}</p>
              <p className="rounded-xl bg-muted/60 p-3 font-medium text-foreground">
                {!paid ? t("notCharged") : free ? t("fullRefund", { amount, deadline }) : t("noRefund", { deadline })}
              </p>
            </div>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            onClick={() =>
              cancel.mutate(booking.id, {
                onSuccess: () => toast.success(t("done"), { description: paid && free ? t("doneRefund", { amount }) : undefined }),
                onError: (error) => toast.error(t("error"), { description: errorMessage(error) }),
              })
            }
            data-testid="experience-cancel-confirm"
          >
            {t("confirm")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

const createReviewSchema = (t: ValidationTranslator) =>
  z.object({
    rating: z.number().int().min(1, t("ratingRequired")).max(5),
    comment: z.string().trim().min(1, t("experienceCommentRequired")).max(COMMENT_MAX, t("commentTooLong", { max: COMMENT_MAX })),
  });
type ReviewValues = z.infer<ReturnType<typeof createReviewSchema>>;

function ReviewExperienceButton({ booking }: { booking: ExperienceBooking }) {
  const t = useTranslations("experiences.trips.review");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const [open, setOpen] = useState(false);
  const review = useReviewExperience();
  const schema = useMemo(() => createReviewSchema(tValidation), [tValidation]);
  const form = useForm<ReviewValues>({ resolver: zodResolver(schema), defaultValues: { rating: 0, comment: "" } });
  const comment = useWatch({ control: form.control, name: "comment" });

  const onSubmit = form.handleSubmit((values) =>
    review.mutate(
      { bookingId: booking.id, ...values },
      {
        onSuccess: () => {
          setOpen(false);
          toast.success(t("thanks"));
        },
        onError: (error) => toast.error(t("error"), { description: errorMessage(error) }),
      },
    ),
  );

  return (
    <>
      <Button variant="outline" onClick={() => setOpen(true)} data-testid="experience-review">
        <Star aria-hidden="true" /> {t("button")}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-lg">
          <form onSubmit={onSubmit} noValidate className="grid gap-5">
            <DialogHeader>
              <DialogTitle>{t("title")}</DialogTitle>
              <DialogDescription>{t("description", { title: booking.experienceTitle })}</DialogDescription>
            </DialogHeader>
            <Controller
              control={form.control}
              name="rating"
              render={({ field, fieldState }) => (
                <StarInput label={t("rating")} value={field.value} onChange={field.onChange} size="lg" showLabel error={fieldState.error?.message} />
              )}
            />
            <Field
              id="experience-review-comment"
              label={t("comment")}
              error={form.formState.errors.comment?.message}
              description={t("charactersLeft", { count: COMMENT_MAX - (comment?.length ?? 0) })}
            >
              {(control) => (
                <Textarea {...control} rows={5} maxLength={COMMENT_MAX} placeholder={t("placeholder")} {...form.register("comment")} />
              )}
            </Field>
            <DialogFooter>
              <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
                {t("cancel")}
              </Button>
              <Button type="submit" disabled={review.isPending} data-testid="experience-review-submit">
                {review.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
                {t("submit")}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}

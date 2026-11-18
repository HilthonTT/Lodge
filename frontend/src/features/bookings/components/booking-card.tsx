"use client";

import { CalendarDays, CircleCheck, CreditCard, Loader2, Star, XCircle } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { RemoteImage } from "@/components/shared/remote-image";
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
import { formatDateRange, nightsBetween, parseDateOnly, today } from "@/lib/dates";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type Booking, BookingStatus } from "@/types/api";

import { useCancelBooking, useCheckout } from "../hooks/use-bookings";
import { BookingStatusBadge } from "./booking-status-badge";
import { ReviewDialog } from "./review-dialog";

export function BookingCard({
  booking,
  highlighted,
  priority,
}: {
  booking: Booking;
  highlighted?: boolean;
  priority?: boolean;
}) {
  const t = useTranslations("bookings");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const hasStarted = start ? start <= today() : true;
  // The API allows cancelling reserved (unpaid) and confirmed bookings before check-in.
  const canCancel =
    (booking.status === BookingStatus.Reserved || booking.status === BookingStatus.Confirmed) && !hasStarted;

  const checkout = useCheckout();
  const cancel = useCancelBooking();
  const [reviewOpen, setReviewOpen] = useState(false);

  const onPay = () =>
    checkout.mutate(booking.id, {
      onError: (error) => toast.error(t("checkoutError"), { description: errorMessage(error) }),
    });

  const onCancel = () =>
    cancel.mutate(booking.id, {
      onSuccess: () => toast.success(t("cancelled")),
      onError: (error) => toast.error(t("cancelError"), { description: errorMessage(error) }),
    });

  return (
    <article
      className={cn(
        "grid gap-4 rounded-2xl border bg-card p-4 shadow-card transition-shadow sm:grid-cols-[11rem_minmax(0,1fr)] sm:gap-5",
        highlighted && "ring-2 ring-primary/60",
      )}
    >
      <Link
        href={`/apartments/${booking.apartmentId}`}
        className="relative aspect-[4/3] overflow-hidden rounded-xl bg-muted sm:aspect-auto sm:h-full sm:min-h-32"
        tabIndex={-1}
        aria-hidden="true"
      >
        <RemoteImage src={booking.apartmentImageUrl} alt="" fill priority={priority} sizes="(min-width: 640px) 176px, 100vw" className="object-cover" />
      </Link>

      <div className="flex min-w-0 flex-col gap-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <h3 className="min-w-0 font-semibold">
            <Link href={`/apartments/${booking.apartmentId}`} className="line-clamp-1 hover:underline">
              {booking.apartmentName}
            </Link>
          </h3>
          <BookingStatusBadge status={booking.status} />
        </div>

        <dl className="grid gap-1.5 text-sm sm:grid-cols-2">
          <div className="flex items-center gap-2 text-muted-foreground">
            <dt className="sr-only">{t("dates")}</dt>
            <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
            <dd>
              {start && end ? formatDateRange(start, end, locale) : "—"}
              {nights > 0 && <span className="ml-1">· {tCommon("nights", { count: nights })}</span>}
            </dd>
          </div>
          <div className="flex items-center gap-2 sm:justify-end">
            <dt className="text-muted-foreground">{t("total")}</dt>
            <dd className="font-semibold tabular-nums">
              {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale)}
            </dd>
          </div>
        </dl>

        <div className="mt-auto flex flex-wrap items-center gap-2 pt-1">
          {booking.status === BookingStatus.Reserved && (
            <>
              <Button onClick={onPay} disabled={checkout.isPending || checkout.isSuccess}>
                {checkout.isPending || checkout.isSuccess ? (
                  <Loader2 className="animate-spin" aria-hidden="true" />
                ) : (
                  <CreditCard aria-hidden="true" />
                )}
                {t("pay")}
              </Button>
              <p className="text-xs text-muted-foreground">{t("payHint")}</p>
            </>
          )}

          {canCancel && (
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="outline" disabled={cancel.isPending}>
                  {cancel.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <XCircle aria-hidden="true" />}
                  {t("cancel")}
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>{t("cancelTitle")}</AlertDialogTitle>
                  <AlertDialogDescription>
                    {t("cancelDescription", {
                      stay:
                        start && end
                          ? t("stayWithDates", { name: booking.apartmentName, dates: formatDateRange(start, end, locale) })
                          : booking.apartmentName,
                    })}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>{t("keep")}</AlertDialogCancel>
                  <AlertDialogAction variant="destructive" onClick={onCancel}>
                    {t("cancel")}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}

          {booking.status === BookingStatus.Completed && booking.hasReview && (
            <p className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
              <CircleCheck className="size-4 text-success" aria-hidden="true" /> {t("reviewed")}
            </p>
          )}

          {booking.status === BookingStatus.Completed && !booking.hasReview && (
            <>
              <Button variant="outline" onClick={() => setReviewOpen(true)}>
                <Star aria-hidden="true" /> {t("writeReview")}
              </Button>
              <ReviewDialog
                bookingId={booking.id}
                apartmentName={booking.apartmentName}
                open={reviewOpen}
                onOpenChange={setReviewOpen}
              />
            </>
          )}
        </div>
      </div>
    </article>
  );
}

"use client";

import {
  CalendarDays,
  CalendarRange,
  CircleCheck,
  CreditCard,
  EyeOff,
  Hourglass,
  KeyRound,
  Loader2,
  MessageSquareText,
  ReceiptText,
  ShieldAlert,
  ShieldCheck,
  Star,
  TimerOff,
  Users,
} from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { RemoteImage } from "@/components/shared/remote-image";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { bookingPriceDetails, PriceBreakdown } from "@/features/apartments/components/price-breakdown";
import { ChargedAmount, ChargedInNote } from "@/features/payments/components/money";
import { useOwnReview } from "@/features/reviews/hooks/use-reviews";
import { ReviewDialog } from "@/features/reviews/components/review-dialog";
import { MessageBookingParty } from "@/features/messaging/components/message-booking-party";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, formatMediumDate, nightsBetween, parseDateOnly, today, utcToday } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type Booking, BookingStatus } from "@/types/api";

import { useCheckout } from "../hooks/use-bookings";
import { BookingStatusBadge } from "./booking-status-badge";
import { CancelBookingButton } from "./cancel-booking-dialog";
import { ChangeDatesDialog } from "./change-dates-dialog";
import { RefundStatusBadge } from "./refund-status";
import { RequestCountdown } from "./request-countdown";

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
  const tPolicy = useTranslations("listing.cancellationPolicies");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const hasStarted = start ? start <= today() : true;
  const isPending = booking.status === BookingStatus.PendingApproval;
  const isUnpaid = isPending || booking.status === BookingStatus.Reserved;
  // The API allows cancelling pending, reserved (unpaid) and confirmed bookings up to and including check-in day.
  const canCancel = (isUnpaid || booking.status === BookingStatus.Confirmed) && (start ? start >= utcToday() : false);
  // Only requests and unpaid reservations can move; confirmed stays must be cancelled instead.
  const canChangeDates = isUnpaid && !hasStarted;
  const isActive = isUnpaid || booking.status === BookingStatus.Confirmed;
  const currency = booking.totalPriceCurrency;

  const checkout = useCheckout();
  const [changeOpen, setChangeOpen] = useState(false);

  const onPay = () =>
    checkout.mutate(booking.id, {
      onError: (error) => toast.error(t("checkoutError"), { description: errorMessage(error) }),
    });

  return (
    <article
      className={cn(
        "grid gap-4 rounded-2xl border bg-card p-4 shadow-card transition-shadow sm:grid-cols-[11rem_minmax(0,1fr)] sm:gap-5",
        highlighted && "ring-2 ring-primary/60",
      )}
      data-booking-id={booking.id}
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
          <div className="flex items-start gap-2 sm:justify-end">
            <dt className="text-muted-foreground">{t("total")}</dt>
            <dd className="font-semibold tabular-nums">
              <ChargedAmount amount={booking.totalPriceAmount} currency={currency} />
            </dd>
          </div>
          {booking.guestCount > 0 && (
            <div className="flex items-center gap-2 text-muted-foreground">
              <dt className="sr-only">{t("guests")}</dt>
              <Users className="size-4 shrink-0" aria-hidden="true" />
              <dd>{tCommon("guests", { count: booking.guestCount })}</dd>
            </div>
          )}
          {isActive && booking.cancellationPolicy && (
            <div className="flex items-center gap-2 text-muted-foreground sm:justify-end">
              <dt className="sr-only">{t("policy")}</dt>
              <ShieldCheck className="size-4 shrink-0" aria-hidden="true" />
              <dd>{t("policyValue", { policy: tPolicy(`${booking.cancellationPolicy}.name`) })}</dd>
            </div>
          )}
        </dl>

        {isUnpaid && <ChargedInNote currency={currency} className="-mt-1" />}

        {isPending && (
          <div className="flex items-start gap-3 rounded-xl bg-info/10 p-3 text-sm">
            <Hourglass className="mt-0.5 size-4 shrink-0 text-info" aria-hidden="true" />
            <p>
              <span className="font-medium">{t("request.pendingTitle")}</span>{" "}
              <span className="text-muted-foreground">{t("request.pendingText")}</span>{" "}
              <RequestCountdown booking={booking} className="font-medium text-info" />
            </p>
          </div>
        )}
        {booking.status === BookingStatus.Expired && (
          <div className="flex items-start gap-3 rounded-xl bg-muted/70 p-3 text-sm">
            <TimerOff className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
            <p>
              <span className="font-medium">{t("request.expiredTitle")}</span>{" "}
              <span className="text-muted-foreground">{t("request.expiredText")}</span>
            </p>
          </div>
        )}
        {booking.status === BookingStatus.Rejected && (
          <p className="text-sm text-muted-foreground">{t("request.declinedText")}</p>
        )}
        {booking.status === BookingStatus.Cancelled && <CancellationSummary booking={booking} />}
        {booking.status === BookingStatus.Confirmed && booking.checkInInstructions && (
          <div className="flex items-start gap-3 rounded-xl border border-primary/25 bg-accent/50 p-3 text-sm">
            <KeyRound className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden="true" />
            <div className="grid gap-0.5">
              <p className="font-medium">{t("checkInInstructions")}</p>
              <p className="whitespace-pre-line text-muted-foreground">{booking.checkInInstructions}</p>
            </div>
          </div>
        )}
        {booking.messageToHost && (
          <details className="group/message text-sm">
            <summary className="flex cursor-pointer list-none items-center gap-2 text-muted-foreground hover:text-foreground">
              <MessageSquareText className="size-4 shrink-0" aria-hidden="true" />
              <span className="underline-offset-4 group-open/message:font-medium hover:underline">{t("yourMessage")}</span>
            </summary>
            <p className="mt-2 rounded-xl bg-muted/60 p-3 whitespace-pre-line">{booking.messageToHost}</p>
          </details>
        )}
        <details className="group/receipt text-sm">
          <summary className="flex cursor-pointer list-none items-center gap-2 text-muted-foreground hover:text-foreground">
            <ReceiptText className="size-4 shrink-0" aria-hidden="true" />
            <span className="underline-offset-4 group-open/receipt:font-medium hover:underline">{t("priceDetails")}</span>
          </summary>
          <div className="mt-2 grid gap-3 rounded-xl bg-muted/40 p-3">
            <PriceBreakdown price={bookingPriceDetails(booking)} />
            {booking.amountPaid !== null && booking.paidOnUtc && (
              <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <CircleCheck className="size-3.5 text-success" aria-hidden="true" />
                {t("paidOn", {
                  amount: formatPrice(booking.amountPaid, currency, locale),
                  date: formatMediumDate(new Date(booking.paidOnUtc), locale),
                })}
              </p>
            )}
          </div>
        </details>

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

          {canChangeDates && (
            <>
              <Button variant="outline" onClick={() => setChangeOpen(true)}>
                <CalendarRange aria-hidden="true" /> {t("changeDates.button")}
              </Button>
              <ChangeDatesDialog booking={booking} open={changeOpen} onOpenChange={setChangeOpen} />
            </>
          )}

          {canCancel && <CancelBookingButton booking={booking} />}

          {booking.status === BookingStatus.Completed && <ReviewActions booking={booking} />}

          <MessageBookingParty booking={booking} as="guest" />
        </div>
      </div>
    </article>
  );
}

/** Refund amount and status of a cancelled booking, and who cancelled it. */
function CancellationSummary({ booking }: { booking: Booking }) {
  const t = useTranslations("bookings");
  const hasRefund = booking.refundAmount !== null && booking.refundAmount > 0;

  return (
    <div
      className={cn(
        "grid gap-2 rounded-xl p-3 text-sm",
        booking.cancelledByHost ? "border border-warning/40 bg-warning/10" : "bg-muted/60",
      )}
    >
      {booking.cancelledByHost && (
        <p className="flex items-start gap-2">
          <ShieldAlert className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true" />
          <span>
            <span className="font-medium">{t("cancelledByHost")}</span>{" "}
            <span className="text-muted-foreground">{t("cancelledByHostText")}</span>
          </span>
        </p>
      )}
      {hasRefund ? (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="flex flex-wrap items-baseline gap-1.5">
            <span className="text-muted-foreground">{t("refund")}</span>
            <ChargedAmount
              amount={booking.refundAmount!}
              currency={booking.totalPriceCurrency}
              className="justify-items-start font-semibold tabular-nums"
            />
          </span>
          <RefundStatusBadge status={booking.refundStatus} />
        </div>
      ) : (
        !booking.cancelledByHost && (
          <p className="text-muted-foreground">{booking.amountPaid ? t("noRefund") : t("notCharged")}</p>
        )
      )}
    </div>
  );
}

/** Write a review, or see / edit your own while it's unpublished. */
function ReviewActions({ booking }: { booking: Booking }) {
  const t = useTranslations("bookings");
  const tReviews = useTranslations("reviews");
  const [open, setOpen] = useState(false);
  const deadline = parseDateOnly(booking.reviewDeadline);
  const windowOpen = deadline ? deadline >= today() : false;
  // Only look the review up while it could still be unpublished (and so editable).
  const own = useOwnReview(booking.apartmentId, booking.id, booking.hasReview && windowOpen);

  const review = own.data;
  const editable = Boolean(review && !review.isPublished);
  if (!booking.hasReview && !booking.canReview) return null;

  return (
    <>
      {!booking.hasReview ? (
        <Button variant="outline" onClick={() => setOpen(true)}>
          <Star aria-hidden="true" /> {t("writeReview")}
        </Button>
      ) : editable ? (
        <>
          <Badge variant="muted">
            <EyeOff aria-hidden="true" /> {tReviews("onlyVisibleToYou")}
          </Badge>
          <Button variant="outline" size="sm" onClick={() => setOpen(true)}>
            {t("editReview")}
          </Button>
        </>
      ) : (
        <p className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
          <CircleCheck className="size-4 text-success" aria-hidden="true" /> {t("reviewed")}
        </p>
      )}
      {/* One dialog instance, so the "thanks" step survives the booking refetch after submitting. */}
      <ReviewDialog
        bookingId={booking.id}
        apartmentName={booking.apartmentName}
        reviewDeadline={booking.reviewDeadline}
        existing={editable ? review : null}
        open={open}
        onOpenChange={setOpen}
      />
    </>
  );
}

"use client";

import { CalendarDays, Check, CircleCheck, Loader2, MessageSquareText, ShieldAlert, Timer, Users, X } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
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
import { BookingStatusBadge } from "@/features/bookings/components/booking-status-badge";
import { RefundStatusBadge } from "@/features/bookings/components/refund-status";
import { requestExpiresAt } from "@/features/bookings/components/request-countdown";
import { MessageBookingParty } from "@/features/messaging/components/message-booking-party";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, nightsBetween, parseDateOnly, today } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type Booking, BookingStatus } from "@/types/api";

import { useAnswerRequest } from "../hooks/use-hosting";
import { useNowMinute } from "../hooks/use-now";
import { ReviewGuestButton } from "./guest-review-dialog";
import { HostCancelButton } from "./host-cancel-dialog";

/** "Expires in 21 h 14 min" countdown for a pending request. */
export function ExpiryCountdown({ booking, className }: { booking: Booking; className?: string }) {
  const t = useTranslations("hosting.request");
  const now = useNowMinute();
  if (!now) return null;
  const left = requestExpiresAt(booking) - now;
  const urgent = left < 3 * 60 * 60 * 1000;
  return (
    <p
      className={cn(
        "inline-flex items-center gap-1.5 text-sm font-medium",
        left <= 0 ? "text-muted-foreground" : urgent ? "text-destructive" : "text-foreground",
        className,
      )}
    >
      <Timer className="size-4" aria-hidden="true" />
      {left <= 0
        ? t("expiring")
        : t("expiresIn", {
            hours: String(Math.floor(left / 3_600_000)),
            minutes: String(Math.floor((left % 3_600_000) / 60_000)),
          })}
    </p>
  );
}

export function RequestActions({ booking, size = "default" }: { booking: Booking; size?: "default" | "sm" }) {
  const t = useTranslations("hosting.request");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const guest = booking.guestName || booking.guestEmail;
  const accept = useAnswerRequest("accept", {
    onSuccess: () => toast.success(t("accepted"), { description: t("acceptedDescription", { guest }) }),
    onError: (error) => toast.error(t("acceptError"), { description: errorMessage(error) }),
  });
  const decline = useAnswerRequest("decline", {
    onSuccess: () => toast.success(t("declined")),
    onError: (error) => toast.error(t("declineError"), { description: errorMessage(error) }),
  });
  const busy = accept.isPending || decline.isPending;

  const onAccept = () => accept.mutate(booking.id);
  const onDecline = () => decline.mutate(booking.id);

  return (
    <div className="flex flex-wrap gap-2">
      <Button size={size} onClick={onAccept} disabled={busy}>
        {accept.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Check aria-hidden="true" />}
        {t("accept")}
      </Button>
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button size={size} variant="outline" disabled={busy}>
            {decline.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <X aria-hidden="true" />}
            {t("decline")}
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("declineTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("declineDescription", { guest, listing: booking.apartmentName })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={onDecline}>
              {t("decline")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

/** A guest's booking or request as the host sees it. */
export function HostBookingCard({ booking, compact }: { booking: Booking; compact?: boolean }) {
  const t = useTranslations("hosting.request");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const pending = booking.status === BookingStatus.PendingApproval;
  const canHostCancel = booking.status === BookingStatus.Confirmed && (start ? start > today() : false);
  const canReviewGuest = booking.status === BookingStatus.Completed && booking.canReview && !booking.hasGuestReview;

  return (
    <article
      className={cn(
        "grid gap-4 rounded-2xl border bg-card p-4 shadow-card sm:grid-cols-[8rem_minmax(0,1fr)] sm:gap-5",
        pending && "border-info/30",
      )}
    >
      <Link
        href={`/apartments/${booking.apartmentId}`}
        tabIndex={-1}
        aria-hidden="true"
        className="relative hidden overflow-hidden rounded-xl bg-muted sm:block sm:min-h-28"
      >
        <RemoteImage src={booking.apartmentImageUrl} alt="" fill sizes="128px" className="object-cover" />
      </Link>
      <div className="flex min-w-0 flex-col gap-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="grid min-w-0 gap-0.5">
            <h3 className="font-semibold">{booking.guestName || booking.guestEmail}</h3>
            <p className="line-clamp-1 text-sm text-muted-foreground">{booking.apartmentName}</p>
          </div>
          <BookingStatusBadge status={booking.status} />
        </div>
        <dl className="flex flex-wrap gap-x-5 gap-y-1.5 text-sm text-muted-foreground">
          <div className="flex items-center gap-1.5">
            <dt className="sr-only">{t("dates")}</dt>
            <CalendarDays className="size-4" aria-hidden="true" />
            <dd>
              {start && end ? formatDateRange(start, end, locale) : "—"}
              {nights > 0 && ` · ${tCommon("nights", { count: nights })}`}
            </dd>
          </div>
          <div className="flex items-center gap-1.5">
            <dt className="sr-only">{t("guests")}</dt>
            <Users className="size-4" aria-hidden="true" />
            <dd>{tCommon("guests", { count: booking.guestCount || 1 })}</dd>
          </div>
          <div className="flex items-center gap-1.5">
            <dt>{t("payout")}</dt>
            <dd className="font-semibold text-foreground tabular-nums">
              {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale)}
            </dd>
          </div>
        </dl>
        {booking.messageToHost && !compact && (
          <blockquote className="flex gap-2 rounded-xl bg-muted/60 p-3 text-sm">
            <MessageSquareText className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
            <p className="whitespace-pre-line">{booking.messageToHost}</p>
          </blockquote>
        )}
        {booking.status === BookingStatus.Cancelled && (booking.cancelledByHost || booking.refundStatus !== "None") && (
          <div className="flex flex-wrap items-center gap-2 text-sm">
            {booking.cancelledByHost && (
              <span className="inline-flex items-center gap-1.5 font-medium text-warning">
                <ShieldAlert className="size-4" aria-hidden="true" /> {t("cancelledByYou")}
              </span>
            )}
            {booking.refundAmount !== null && booking.refundAmount > 0 && (
              <span className="text-muted-foreground">
                {t("refunded", { amount: formatPrice(booking.refundAmount, booking.totalPriceCurrency, locale) })}
              </span>
            )}
            <RefundStatusBadge status={booking.refundStatus} />
          </div>
        )}
        {pending && (
          <div className="mt-auto flex flex-wrap items-center justify-between gap-3">
            <ExpiryCountdown booking={booking} />
            <RequestActions booking={booking} size="sm" />
          </div>
        )}
        {!compact && (
          <div className="mt-auto flex flex-wrap items-center gap-2">
            <MessageBookingParty booking={booking} as="host" size="sm" />
            {canReviewGuest && <ReviewGuestButton booking={booking} />}
            {booking.status === BookingStatus.Completed && booking.hasGuestReview && (
              <p className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
                <CircleCheck className="size-4 text-success" aria-hidden="true" /> {t("guestReviewed")}
              </p>
            )}
            {canHostCancel && <HostCancelButton booking={booking} />}
          </div>
        )}
      </div>
    </article>
  );
}

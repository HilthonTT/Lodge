"use client";

import { AlertCircle, Info, Loader2, XCircle } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { ChargedAmount } from "@/features/payments/components/money";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, parseDateOnly, today } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { type Booking, BookingStatus, type CancellationQuote } from "@/types/api";

import { useCancelBooking, useCancellationQuote } from "../hooks/use-bookings";
import { quoteTiers, RefundTimeline } from "./refund-timeline";

/** Index of the quote tier that applies today. */
function currentTier(quote: CancellationQuote) {
  const now = today();
  const tiers = quoteTiers(quote.timeline);
  const index = tiers.findIndex((tier) => tier.until === null || tier.until >= now);
  return index === -1 ? tiers.length - 1 : index;
}

/**
 * Cancel button + confirmation that first asks the API what cancelling now
 * would refund (`/bookings/{id}/cancellation-quote`).
 */
export function CancelBookingButton({ booking }: { booking: Booking }) {
  const t = useTranslations("bookings");
  const [open, setOpen] = useState(false);
  const isRequest = booking.status === BookingStatus.PendingApproval;

  return (
    <>
      <Button variant="outline" onClick={() => setOpen(true)}>
        <XCircle aria-hidden="true" />
        {isRequest ? t("request.cancel") : t("cancel")}
      </Button>
      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-lg">
          {open && <CancelBody booking={booking} onDone={() => setOpen(false)} />}
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

function CancelBody({ booking, onDone }: { booking: Booking; onDone: () => void }) {
  const t = useTranslations("bookings");
  const tCancel = useTranslations("cancellation");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const quote = useCancellationQuote(booking.id, true);
  const cancel = useCancelBooking();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const stay =
    start && end
      ? t("stayWithDates", { name: booking.apartmentName, dates: formatDateRange(start, end, locale) })
      : booking.apartmentName;

  const onConfirm = () =>
    cancel.mutate(booking.id, {
      onSuccess: () => {
        const q = quote.data;
        toast.success(t("cancelled"), {
          description:
            q?.isPaid && q.refundAmount > 0
              ? t("refundOnTheWay", { amount: formatPrice(q.refundAmount, q.currency, locale) })
              : undefined,
        });
        onDone();
      },
    });

  return (
    <>
      <AlertDialogHeader>
        <AlertDialogTitle>{t("cancelTitle")}</AlertDialogTitle>
        <AlertDialogDescription>{t("cancelDescription", { stay })}</AlertDialogDescription>
      </AlertDialogHeader>

      <div aria-live="polite" className="grid gap-4">
        {quote.isPending ? (
          <div className="grid gap-3" role="status" aria-label={tCancel("loadingQuote")}>
            <Skeleton className="h-16 w-full rounded-xl" />
            <Skeleton className="h-24 w-full rounded-xl" />
          </div>
        ) : quote.isError ? (
          <p role="alert" className="flex items-start gap-2 rounded-xl bg-destructive/10 p-3 text-sm text-destructive">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {errorMessage(quote.error)}
          </p>
        ) : (
          <QuoteDetails quote={quote.data} />
        )}
        {cancel.error && (
          <p role="alert" className="flex items-start gap-2 text-sm text-destructive">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {errorMessage(cancel.error)}
          </p>
        )}
      </div>

      <AlertDialogFooter>
        <AlertDialogCancel>{t("keep")}</AlertDialogCancel>
        {/* Not an AlertDialogAction: the dialog stays open until the API confirms. */}
        <Button variant="destructive" onClick={onConfirm} disabled={!quote.data || cancel.isPending}>
          {cancel.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {quote.data?.isPaid ? t("cancelAndRefund") : t("cancel")}
        </Button>
      </AlertDialogFooter>
    </>
  );
}

function QuoteDetails({ quote }: { quote: CancellationQuote }) {
  const t = useTranslations("cancellation");
  const tPolicy = useTranslations("listing.cancellationPolicies");

  return (
    <div className="grid gap-4">
      {quote.isPaid ? (
        <dl className="grid gap-2 rounded-xl border p-4 text-sm">
          <div className="flex items-start justify-between gap-4">
            <dt className="font-medium">{t("refundAmount")}</dt>
            <dd className="text-right font-semibold text-success tabular-nums">
              <ChargedAmount amount={quote.refundAmount} currency={quote.currency} />
            </dd>
          </div>
          <div className="flex items-start justify-between gap-4">
            <dt className="text-muted-foreground">{t("nonRefundable")}</dt>
            <dd className="text-right tabular-nums">
              <ChargedAmount amount={quote.nonRefundableAmount} currency={quote.currency} />
            </dd>
          </div>
          <Separator className="my-1" />
          <p className="text-xs text-muted-foreground">
            {t("quoteSummary", { percent: String(quote.refundPercent), policy: tPolicy(`${quote.policy}.name`) })}{" "}
            {t("feesNote")}
          </p>
        </dl>
      ) : (
        <p className="flex items-start gap-2 rounded-xl bg-info/10 p-3 text-sm">
          <Info className="mt-0.5 size-4 shrink-0 text-info" aria-hidden="true" />
          {t("unpaid")}
        </p>
      )}
      <div className="grid gap-3">
        <p className="text-sm font-semibold">{t("timelineTitle", { policy: tPolicy(`${quote.policy}.name`) })}</p>
        <RefundTimeline tiers={quoteTiers(quote.timeline)} currentIndex={currentTier(quote)} />
      </div>
    </div>
  );
}

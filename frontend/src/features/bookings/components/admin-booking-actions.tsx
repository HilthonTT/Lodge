"use client";

import { Ban, CircleCheckBig, Loader2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { toast } from "sonner";

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
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, parseDateOnly, today } from "@/lib/dates";
import { type Booking, BookingStatus } from "@/types/api";

import { useAdminBookingAction } from "../hooks/use-admin-bookings";

/** Mirrors the API rules: reject while reserved; complete once a confirmed stay has ended. */
export function getAdminActions(booking: Booking) {
  const end = parseDateOnly(booking.durationEnd);
  return {
    canReject: booking.status === BookingStatus.Reserved || booking.status === BookingStatus.PendingApproval,
    canComplete: booking.status === BookingStatus.Confirmed && !!end && end <= today(),
  };
}

export function AdminBookingActions({ booking }: { booking: Booking }) {
  const t = useTranslations("admin");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const { canReject, canComplete } = getAdminActions(booking);
  const reject = useAdminBookingAction("reject");
  const complete = useAdminBookingAction("complete");

  if (!canReject && !canComplete) {
    return <span className="text-sm text-muted-foreground">—</span>;
  }

  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const guest = booking.guestName || booking.guestEmail;
  const stay =
    start && end
      ? t("stayWithDates", { guest, apartment: booking.apartmentName, dates: formatDateRange(start, end, locale) })
      : t("stay", { guest, apartment: booking.apartmentName });

  const onReject = () =>
    reject.mutate(booking.id, {
      onSuccess: () => toast.success(t("rejected"), { description: stay }),
      onError: (error) => toast.error(t("rejectError"), { description: errorMessage(error) }),
    });

  const onComplete = () =>
    complete.mutate(booking.id, {
      onSuccess: () => toast.success(t("completed"), { description: stay }),
      onError: (error) => toast.error(t("completeError"), { description: errorMessage(error) }),
    });

  return (
    <div className="flex flex-wrap items-center gap-2">
      {canReject && (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="outline" size="sm" disabled={reject.isPending}>
              {reject.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Ban aria-hidden="true" />}
              {t("reject")}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t("rejectTitle")}</AlertDialogTitle>
              <AlertDialogDescription>
                {t("rejectDescription", { stay })}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t("keep")}</AlertDialogCancel>
              <AlertDialogAction variant="destructive" onClick={onReject}>
                {t("rejectConfirm")}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}

      {canComplete && (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="outline" size="sm" disabled={complete.isPending}>
              {complete.isPending ? (
                <Loader2 className="animate-spin" aria-hidden="true" />
              ) : (
                <CircleCheckBig aria-hidden="true" />
              )}
              {t("markCompleted")}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t("completeTitle")}</AlertDialogTitle>
              <AlertDialogDescription>
                {t("completeDescription", { stay })}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t("notYet")}</AlertDialogCancel>
              <AlertDialogAction onClick={onComplete}>{t("markCompleted")}</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}
    </div>
  );
}

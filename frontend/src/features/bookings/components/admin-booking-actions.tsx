"use client";

import { Ban, CircleCheckBig, Loader2 } from "lucide-react";
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
import { formatDateRange, parseDateOnly, today } from "@/lib/dates";
import { type Booking, BookingStatus } from "@/types/api";

import { useAdminBookingAction } from "../hooks/use-admin-bookings";

/** Mirrors the API rules: reject while reserved; complete once a confirmed stay has ended. */
export function getAdminActions(booking: Booking) {
  const end = parseDateOnly(booking.durationEnd);
  return {
    canReject: booking.status === BookingStatus.Reserved,
    canComplete: booking.status === BookingStatus.Confirmed && !!end && end <= today(),
  };
}

export function AdminBookingActions({ booking }: { booking: Booking }) {
  const { canReject, canComplete } = getAdminActions(booking);
  const reject = useAdminBookingAction("reject");
  const complete = useAdminBookingAction("complete");

  if (!canReject && !canComplete) {
    return <span className="text-sm text-muted-foreground">—</span>;
  }

  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const stay = `${booking.guestName || booking.guestEmail}'s stay at ${booking.apartmentName}${
    start && end ? ` (${formatDateRange(start, end)})` : ""
  }`;

  const onReject = () =>
    reject.mutate(booking.id, {
      onSuccess: () => toast.success("Booking rejected", { description: stay }),
      onError: (error) => toast.error("Couldn't reject this booking", { description: error.message }),
    });

  const onComplete = () =>
    complete.mutate(booking.id, {
      onSuccess: () => toast.success("Stay marked as completed", { description: stay }),
      onError: (error) => toast.error("Couldn't complete this booking", { description: error.message }),
    });

  return (
    <div className="flex flex-wrap items-center gap-2">
      {canReject && (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="outline" size="sm" disabled={reject.isPending}>
              {reject.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Ban aria-hidden="true" />}
              Reject
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Reject this booking?</AlertDialogTitle>
              <AlertDialogDescription>
                {stay} will be rejected and the dates released. The guest hasn&apos;t paid yet. This
                can&apos;t be undone.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Keep booking</AlertDialogCancel>
              <AlertDialogAction variant="destructive" onClick={onReject}>
                Reject booking
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
              Mark completed
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Mark this stay as completed?</AlertDialogTitle>
              <AlertDialogDescription>
                {stay} will be marked as completed, and the guest will be able to leave a review.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Not yet</AlertDialogCancel>
              <AlertDialogAction onClick={onComplete}>Mark completed</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}
    </div>
  );
}

"use client";

import { AlertCircle, ArrowRight, Loader2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import type { DateRange } from "react-day-picker";
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
import { Skeleton } from "@/components/ui/skeleton";
import { PriceBreakdown } from "@/features/apartments/components/price-breakdown";
import { StayCalendar, stayLengthIssue } from "@/features/apartments/components/stay-calendar";
import { useApartment, useBookedDates } from "@/features/apartments/hooks/use-apartment";
import { useApartmentCalendar } from "@/features/apartments/hooks/use-apartment-calendar";
import { useApartmentPrice } from "@/features/apartments/hooks/use-apartment-price";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, nightsBetween, parseDateOnly, type StayRange, toDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import type { Booking } from "@/types/api";

import { useChangeBookingDates } from "../hooks/use-bookings";

type ChangeDatesDialogProps = {
  booking: Booking;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/** Pick new dates for a pending or reserved booking and see the new price before confirming. */
export function ChangeDatesDialog({ booking, open, onOpenChange }: ChangeDatesDialogProps) {
  const t = useTranslations("bookings.changeDates");
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[46rem]">
        <DialogHeader>
          <DialogTitle>{t("title")}</DialogTitle>
          <DialogDescription>{t("description", { name: booking.apartmentName })}</DialogDescription>
        </DialogHeader>
        {open && <ChangeDatesForm booking={booking} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function ChangeDatesForm({ booking, onDone }: { booking: Booking; onDone: () => void }) {
  const t = useTranslations("bookings.changeDates");
  const tCommon = useTranslations("common");
  const tBooking = useTranslations("apartment.booking");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const apartment = useApartment(booking.apartmentId);
  const booked = useBookedDates(booking.apartmentId);
  const calendar = useApartmentCalendar(booking.apartmentId);
  const change = useChangeBookingDates();

  const current = useMemo<StayRange | undefined>(() => {
    const from = parseDateOnly(booking.durationStart);
    const to = parseDateOnly(booking.durationEnd);
    return from && to ? { from, to } : undefined;
  }, [booking.durationStart, booking.durationEnd]);
  const [range, setRange] = useState<DateRange | undefined>(current);

  const minNights = Math.max(1, apartment.data?.minNights ?? 1);
  const maxNights = apartment.data?.maxNights || 365;
  const stay =
    range?.from && range.to && nightsBetween(range.from, range.to) > 0 ? { from: range.from, to: range.to } : undefined;
  const unchanged =
    stay && current && toDateOnly(stay.from) === toDateOnly(current.from) && toDateOnly(stay.to) === toDateOnly(current.to);
  const lengthIssue = stayLengthIssue(stay, minNights, maxNights);
  const valid = stay && !lengthIssue && !unchanged ? stay : undefined;
  const price = useApartmentPrice(booking.apartmentId, valid);

  const onConfirm = () => {
    if (!valid) return;
    change.mutate(
      {
        bookingId: booking.id,
        apartmentId: booking.apartmentId,
        startDate: toDateOnly(valid.from),
        endDate: toDateOnly(valid.to),
      },
      {
        onSuccess: () => {
          toast.success(t("success"), { description: formatDateRange(valid.from, valid.to, locale) });
          onDone();
        },
      },
    );
  };

  if (apartment.isPending || booked.isPending) {
    return (
      <div className="grid gap-3" role="status" aria-label={t("loading")}>
        <Skeleton className="h-72 w-full rounded-2xl" />
      </div>
    );
  }

  return (
    <div className="grid gap-5">
      <div className="grid gap-6 md:grid-cols-[auto_minmax(0,1fr)]">
        <div className="flex justify-center">
          <StayCalendar
            selected={range}
            onSelect={setRange}
            days={calendar.data}
            bookedDates={booked.data ?? []}
            currency={apartment.data?.currency ?? booking.totalPriceCurrency}
            minNights={minNights}
            maxNights={maxNights}
            ownStay={current}
            numberOfMonths={1}
            autoFocus
          />
        </div>
        <div className="grid content-start gap-4 text-sm">
          <div className="grid gap-1 rounded-xl bg-muted/60 p-3">
            <p className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">{t("current")}</p>
            <p>{current ? formatDateRange(current.from, current.to, locale) : "—"}</p>
            <p className="font-medium tabular-nums">
              {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale)}
            </p>
          </div>
          <div className="grid gap-1 rounded-xl border p-3">
            <p className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">{t("new")}</p>
            <p aria-live="polite">
              {stay
                ? `${formatDateRange(stay.from, stay.to, locale)} · ${tCommon("nights", { count: nightsBetween(stay.from, stay.to) })}`
                : range?.from
                  ? tBooking("selectCheckOut")
                  : tBooking("selectCheckIn")}
            </p>
            {valid &&
              (price.data ? (
                <div className="mt-2 grid gap-2">
                  <PriceBreakdown price={price.data} className={price.isPlaceholderData ? "opacity-60" : undefined} />
                  {price.data.totalPrice !== booking.totalPriceAmount && (
                    <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale)}
                      <ArrowRight className="size-3" aria-hidden="true" />
                      <span className="font-semibold text-foreground">
                        {formatPrice(price.data.totalPrice, price.data.currency, locale)}
                      </span>
                    </p>
                  )}
                </div>
              ) : price.isError ? (
                <p className="text-destructive">{errorMessage(price.error)}</p>
              ) : (
                <Skeleton className="mt-2 h-16 w-full" />
              ))}
          </div>
          {minNights > 1 && <p className="text-xs text-muted-foreground">{tBooking("minNights", { count: minNights })}</p>}
          {lengthIssue && (
            <p role="alert" className="flex items-start gap-2 text-destructive">
              <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              {lengthIssue === "tooShort"
                ? tBooking("tooShort", { count: minNights })
                : tBooking("tooLong", { count: maxNights })}
            </p>
          )}
          {change.error && (
            <p role="alert" className="flex items-start gap-2 text-destructive">
              <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              {errorMessage(change.error)}
            </p>
          )}
        </div>
      </div>
      <DialogFooter>
        <Button variant="ghost" onClick={onDone}>
          {tCommon("cancel")}
        </Button>
        <Button onClick={onConfirm} disabled={!valid || !price.data || change.isPending}>
          {change.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {price.data && valid
            ? t("confirmWithPrice", { price: formatPrice(price.data.totalPrice, price.data.currency, locale) })
            : t("confirm")}
        </Button>
      </DialogFooter>
    </div>
  );
}

"use client";

import { AlertCircle, ChevronDown, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useRef, useState } from "react";
import type { DateRange } from "react-day-picker";

import { Stepper } from "@/components/shared/stepper";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/features/auth/session-provider";
import { useMediaQuery } from "@/hooks/use-media-query";
import { loginUrl } from "@/lib/auth/routes";
import {
  formatShortDate,
  nightsBetween,
  parseDateOnly,
  parseStayRange,
  type StayRange,
  toDateOnly,
  today,
} from "@/lib/dates";
import { formatPrice, formatPriceCompact, pluralize } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { Apartment } from "@/types/api";

import { useApartmentPrice } from "../hooks/use-apartment-price";
import { PriceBreakdown } from "./price-breakdown";

type BookingPanelProps = {
  apartment: Pick<Apartment, "id" | "price" | "currency" | "maximumGuestCount">;
  /** Booked nights from the API ("yyyy-MM-ddT00:00:00"); check-out days are not included. */
  bookedDates: string[];
  bookedDatesUnavailable?: boolean;
  initialStartDate?: string;
  initialEndDate?: string;
  initialGuests?: number;
};

export function BookingPanel({
  apartment,
  bookedDates,
  bookedDatesUnavailable,
  initialStartDate,
  initialEndDate,
  initialGuests,
}: BookingPanelProps) {
  const router = useRouter();
  const { isAuthenticated } = useSession();
  const isDesktop = useMediaQuery("(min-width: 1024px)");
  const panelRef = useRef<HTMLDivElement>(null);

  const maxGuests = Math.max(1, apartment.maximumGuestCount || 1);
  const [range, setRange] = useState<DateRange | undefined>(() =>
    parseStayRange(initialStartDate, initialEndDate),
  );
  const [guests, setGuests] = useState(() => Math.min(Math.max(initialGuests ?? 1, 1), maxGuests));
  const [datesOpen, setDatesOpen] = useState(false);
  const [guestsOpen, setGuestsOpen] = useState(false);

  const booked = useMemo(
    () =>
      bookedDates
        .map((d) => parseDateOnly(d))
        .filter((d): d is Date => d !== undefined),
    [bookedDates],
  );
  const bookedKeys = useMemo(() => new Set(booked.map(toDateOnly)), [booked]);

  const stay = useMemo<StayRange | undefined>(
    () =>
      range?.from && range.to && nightsBetween(range.from, range.to) > 0
        ? { from: range.from, to: range.to }
        : undefined,
    [range],
  );

  // A range restored from the URL may overlap dates that got booked since.
  const conflict = useMemo(() => {
    if (!stay) return false;
    // Only the nights matter: the check-out day itself may be another stay's first night.
    for (let d = new Date(stay.from); d < stay.to; d.setDate(d.getDate() + 1)) {
      if (bookedKeys.has(toDateOnly(d))) return true;
    }
    return false;
  }, [stay, bookedKeys]);

  const validStay = stay && !conflict ? stay : undefined;

  // While picking check-out, allow any day up to and including the first booked night
  // after check-in (guests leave in the morning, the next guest arrives that afternoon).
  const disabledDays = useMemo(() => {
    const past = { before: today() };
    if (range?.from && !range.to) {
      const from = range.from;
      const nextBooked = booked.find((d) => d > from);
      return [past, ...booked.filter((d) => d < from), ...(nextBooked ? [{ after: nextBooked }] : [])];
    }
    return [past, ...booked];
  }, [range, booked]);
  const nights = validStay ? nightsBetween(validStay.from, validStay.to) : 0;
  const priceQuery = useApartmentPrice(apartment.id, validStay);

  /** Keep the URL shareable without re-rendering the server page. */
  const syncUrl = (nextRange: DateRange | undefined, nextGuests: number) => {
    const url = new URL(window.location.href);
    if (nextRange?.from && nextRange.to && nightsBetween(nextRange.from, nextRange.to) > 0) {
      url.searchParams.set("startDate", toDateOnly(nextRange.from));
      url.searchParams.set("endDate", toDateOnly(nextRange.to));
    } else {
      url.searchParams.delete("startDate");
      url.searchParams.delete("endDate");
    }
    if (nextGuests > 1) url.searchParams.set("guests", String(nextGuests));
    else url.searchParams.delete("guests");
    window.history.replaceState(null, "", url);
  };

  const onSelectRange = (next: DateRange | undefined) => {
    setRange(next);
    syncUrl(next, guests);
    if (next?.from && next.to && nightsBetween(next.from, next.to) > 0) setDatesOpen(false);
  };

  const onGuestsChange = (value: number) => {
    setGuests(value);
    syncUrl(range, value);
  };

  const onReserve = () => {
    if (!validStay) {
      panelRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
      setDatesOpen(true);
      return;
    }
    const params = new URLSearchParams({
      startDate: toDateOnly(validStay.from),
      endDate: toDateOnly(validStay.to),
      guests: String(guests),
    });
    const target = `/stays/${apartment.id}?${params}`;
    router.push(isAuthenticated ? target : loginUrl(target));
  };

  const field = "grid gap-0.5 px-4 py-3 text-left transition-colors hover:bg-muted/60 focus-visible:bg-muted/60 focus-visible:outline-none";

  return (
    <>
      <div
        ref={panelRef}
        id="booking"
        className="grid gap-5 rounded-2xl border bg-card p-6 shadow-float scroll-mt-28"
      >
        <p className="flex items-baseline gap-1.5">
          <span className="text-2xl font-semibold">
            {formatPriceCompact(apartment.price, apartment.currency)}
          </span>
          <span className="text-muted-foreground">night</span>
        </p>

        <div className="overflow-hidden rounded-xl border">
          <Popover open={datesOpen} onOpenChange={setDatesOpen}>
            <PopoverTrigger asChild>
              <button
                type="button"
                className="grid w-full grid-cols-2 divide-x border-b"
                aria-label="Choose check-in and check-out dates"
              >
                <span className={field}>
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">Check-in</span>
                  <span className={cn("text-sm", !range?.from && "text-muted-foreground")}>
                    {range?.from ? formatShortDate(range.from) : "Add date"}
                  </span>
                </span>
                <span className={field}>
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">Check-out</span>
                  <span className={cn("text-sm", !range?.to && "text-muted-foreground")}>
                    {range?.to ? formatShortDate(range.to) : "Add date"}
                  </span>
                </span>
              </button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-auto p-4">
              <div className="mb-3 grid gap-0.5">
                <p className="font-semibold">
                  {validStay ? pluralize(nights, "night") : range?.from ? "Select check-out" : "Select check-in"}
                </p>
                <p className="text-xs text-muted-foreground">
                  Crossed-out nights are booked. You can still check out on one.
                </p>
              </div>
              <Calendar
                mode="range"
                numberOfMonths={isDesktop ? 2 : 1}
                selected={range}
                onSelect={onSelectRange}
                disabled={disabledDays}
                modifiers={{ booked }}
                modifiersClassNames={{ booked: "[&>button]:line-through" }}
                startMonth={today()}
                defaultMonth={range?.from ?? today()}
                autoFocus
              />
              <div className="mt-3 flex justify-end gap-2">
                <Button variant="ghost" size="sm" onClick={() => onSelectRange(undefined)}>
                  Clear dates
                </Button>
                <Button size="sm" onClick={() => setDatesOpen(false)}>
                  Close
                </Button>
              </div>
            </PopoverContent>
          </Popover>

          <Popover open={guestsOpen} onOpenChange={setGuestsOpen}>
            <PopoverTrigger asChild>
              <button type="button" className={cn(field, "flex w-full items-center justify-between")}>
                <span className="grid gap-0.5">
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">Guests</span>
                  <span className="text-sm">{pluralize(guests, "guest")}</span>
                </span>
                <ChevronDown className="size-4 text-muted-foreground" aria-hidden="true" />
              </button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-80">
              <Stepper
                id="booking-guests"
                label="Guests"
                description={`This place allows up to ${pluralize(maxGuests, "guest")}`}
                value={guests}
                max={maxGuests}
                onChange={onGuestsChange}
              />
            </PopoverContent>
          </Popover>
        </div>

        {conflict && (
          <p role="alert" className="flex items-start gap-2 text-sm text-destructive">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            Some of those dates are no longer available. Please pick different dates.
          </p>
        )}
        {bookedDatesUnavailable && (
          <p className="text-xs text-muted-foreground">
            Availability couldn&apos;t be loaded; unavailable dates will be rejected when you reserve.
          </p>
        )}

        <Button size="lg" className="w-full" onClick={onReserve}>
          {validStay ? "Reserve" : "Check availability"}
        </Button>

        <div aria-live="polite" className="grid gap-3">
          {validStay ? (
            priceQuery.isError ? (
              <p className="flex items-start gap-2 text-sm text-destructive">
                <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
                {priceQuery.error.message}
              </p>
            ) : priceQuery.data ? (
              <>
                <p className="text-center text-xs text-muted-foreground">You won&apos;t be charged yet</p>
                <PriceBreakdown
                  price={priceQuery.data}
                  nights={nights}
                  className={cn(priceQuery.isPlaceholderData && "opacity-60 transition-opacity")}
                />
              </>
            ) : (
              <div className="grid gap-3" role="status" aria-label="Calculating price">
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-5/6" />
                <Skeleton className="h-5 w-full" />
              </div>
            )
          ) : (
            <p className="text-center text-sm text-muted-foreground">
              Add your travel dates for the exact total.
            </p>
          )}
        </div>
      </div>

      {/* Mobile: persistent price + CTA bar */}
      <div className="fixed inset-x-0 bottom-0 z-30 border-t bg-background/95 px-4 py-3 backdrop-blur lg:hidden">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
          <div className="grid leading-tight">
            {validStay && priceQuery.data ? (
              <>
                <span className="font-semibold">
                  {formatPrice(priceQuery.data.totalPrice, priceQuery.data.currency)}
                </span>
                <span className="text-xs text-muted-foreground">
                  {formatShortDate(validStay.from)} – {formatShortDate(validStay.to)}
                </span>
              </>
            ) : (
              <>
                <span className="font-semibold">
                  {formatPriceCompact(apartment.price, apartment.currency)}{" "}
                  <span className="font-normal text-muted-foreground">night</span>
                </span>
                <span className="text-xs text-muted-foreground">Add dates for total</span>
              </>
            )}
          </div>
          <Button onClick={onReserve} disabled={Boolean(validStay) && priceQuery.isFetching && !priceQuery.data}>
            {validStay && priceQuery.isFetching && !priceQuery.data && <Loader2 className="animate-spin" />}
            {validStay ? "Reserve" : "Check availability"}
          </Button>
        </div>
      </div>
    </>
  );
}

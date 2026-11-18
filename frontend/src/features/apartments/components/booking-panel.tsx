"use client";

import { AlertCircle, ChevronDown, Info, Loader2, Zap } from "lucide-react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useRef, useState } from "react";
import type { DateRange } from "react-day-picker";

import { Stepper } from "@/components/shared/stepper";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/features/auth/session-provider";
import { ChargedInNote, Money } from "@/features/payments/components/money";
import { useMediaQuery } from "@/hooks/use-media-query";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { loginUrl } from "@/lib/auth/routes";
import { formatShortDate, nightsBetween, parseDateOnly, parseStayRange, type StayRange, toDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Apartment } from "@/types/api";

import { useApartmentCalendar } from "../hooks/use-apartment-calendar";
import { useApartmentPrice } from "../hooks/use-apartment-price";
import { PriceBreakdown } from "./price-breakdown";
import { StayCalendar, stayLengthIssue } from "./stay-calendar";

type BookingPanelProps = {
  apartment: Pick<
    Apartment,
    "id" | "price" | "currency" | "maximumGuestCount" | "instantBook" | "minNights" | "maxNights" | "weeklyDiscountPercent" | "monthlyDiscountPercent"
  >;
  /** Unavailable nights from the API ("yyyy-MM-ddT00:00:00"); check-out days are not included. */
  bookedDates: string[];
  bookedDatesUnavailable?: boolean;
  initialStartDate?: string;
  initialEndDate?: string;
  initialGuests?: number;
  /** The signed-in user hosts this listing: show a note instead of the CTA. */
  isOwnListing?: boolean;
};

export function BookingPanel({
  apartment,
  bookedDates,
  bookedDatesUnavailable,
  initialStartDate,
  initialEndDate,
  initialGuests,
  isOwnListing,
}: BookingPanelProps) {
  const t = useTranslations("apartment.booking");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const { isAuthenticated } = useSession();
  const isDesktop = useMediaQuery("(min-width: 1024px)");
  const panelRef = useRef<HTMLDivElement>(null);
  const calendar = useApartmentCalendar(apartment.id);
  const minNights = Math.max(1, apartment.minNights || 1);
  const maxNights = apartment.maxNights || 365;

  const maxGuests = Math.max(1, apartment.maximumGuestCount || 1);
  const [range, setRange] = useState<DateRange | undefined>(() =>
    parseStayRange(initialStartDate, initialEndDate),
  );
  const [guests, setGuests] = useState(() => Math.min(Math.max(initialGuests ?? 1, 1), maxGuests));
  const [datesOpen, setDatesOpen] = useState(false);
  const [guestsOpen, setGuestsOpen] = useState(false);

  const unavailableKeys = useMemo(() => {
    const keys = new Set<string>();
    for (const d of bookedDates) {
      const date = parseDateOnly(d);
      if (date) keys.add(toDateOnly(date));
    }
    if (calendar.data) for (const day of calendar.data.values()) if (!day.available) keys.add(day.date);
    return keys;
  }, [bookedDates, calendar.data]);

  const stay = useMemo<StayRange | undefined>(
    () =>
      range?.from && range.to && nightsBetween(range.from, range.to) > 0
        ? { from: range.from, to: range.to }
        : undefined,
    [range],
  );

  // A range restored from the URL may overlap nights that got booked or blocked since.
  const conflict = useMemo(() => {
    if (!stay) return false;
    // Only the nights matter: the check-out day itself may be another stay's first night.
    for (let d = new Date(stay.from); d < stay.to; d.setDate(d.getDate() + 1)) {
      if (unavailableKeys.has(toDateOnly(d))) return true;
    }
    return false;
  }, [stay, unavailableKeys]);

  const lengthIssue = stayLengthIssue(stay, minNights, maxNights);
  const validStay = stay && !conflict && !lengthIssue ? stay : undefined;
  const nights = validStay ? nightsBetween(validStay.from, validStay.to) : 0;
  const priceQuery = useApartmentPrice(apartment.id, validStay);
  const ctaLabel = apartment.instantBook ? t("reserve") : t("requestToBook");

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

  const field =
    "grid gap-0.5 px-4 py-3 text-left transition-colors hover:bg-muted/60 focus-visible:bg-muted/60 focus-visible:outline-none";

  return (
    <>
      <div ref={panelRef} id="booking" className="grid scroll-mt-28 gap-5 rounded-2xl border bg-card p-6 shadow-float">
        <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
          <p className="flex items-baseline gap-1.5">
            {validStay && priceQuery.data ? (
              <>
                <Money
                  amount={priceQuery.data.averageNightlyPrice}
                  currency={priceQuery.data.currency}
                  compact
                  tooltip
                  className="text-2xl font-semibold"
                />
                <span className="text-muted-foreground">{t("perNightAverage")}</span>
              </>
            ) : (
              <>
                <Money amount={apartment.price} currency={apartment.currency} compact tooltip className="text-2xl font-semibold" />
                <span className="text-muted-foreground">{tCommon("perNight")}</span>
              </>
            )}
          </p>
          {apartment.instantBook && (
            <span className="inline-flex items-center gap-1 text-xs font-medium text-primary">
              <Zap className="size-3.5 fill-current" aria-hidden="true" /> {t("instantBook")}
            </span>
          )}
        </div>

        <div className="overflow-hidden rounded-xl border">
          <Popover open={datesOpen} onOpenChange={setDatesOpen}>
            <PopoverTrigger asChild>
              <button type="button" className="grid w-full grid-cols-2 divide-x border-b" aria-label={t("chooseDates")}>
                <span className={field}>
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">{t("checkIn")}</span>
                  <span className={cn("text-sm", !range?.from && "text-muted-foreground")}>
                    {range?.from ? formatShortDate(range.from, locale) : t("addDate")}
                  </span>
                </span>
                <span className={field}>
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">{t("checkOut")}</span>
                  <span className={cn("text-sm", !range?.to && "text-muted-foreground")}>
                    {range?.to ? formatShortDate(range.to, locale) : t("addDate")}
                  </span>
                </span>
              </button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-auto max-w-[calc(100vw-1rem)] p-4">
              <div className="mb-3 grid gap-0.5">
                <p className="font-semibold">
                  {validStay
                    ? tCommon("nights", { count: nights })
                    : range?.from
                      ? t("selectCheckOut")
                      : t("selectCheckIn")}
                </p>
                <p className="text-xs text-muted-foreground">
                  {minNights > 1 ? `${t("minNights", { count: minNights })} · ` : ""}
                  {t("bookedHint")}
                </p>
              </div>
              <StayCalendar
                selected={range}
                onSelect={onSelectRange}
                days={calendar.data}
                bookedDates={bookedDates}
                currency={apartment.currency}
                minNights={minNights}
                maxNights={maxNights}
                numberOfMonths={isDesktop ? 2 : 1}
                autoFocus
              />
              <div className="mt-3 flex justify-end gap-2">
                <Button variant="ghost" size="sm" onClick={() => onSelectRange(undefined)}>
                  {t("clearDates")}
                </Button>
                <Button size="sm" onClick={() => setDatesOpen(false)}>
                  {tCommon("close")}
                </Button>
              </div>
            </PopoverContent>
          </Popover>

          <Popover open={guestsOpen} onOpenChange={setGuestsOpen}>
            <PopoverTrigger asChild>
              <button type="button" className={cn(field, "flex w-full items-center justify-between")}>
                <span className="grid gap-0.5">
                  <span className="text-[0.65rem] font-semibold tracking-wide uppercase">{t("guests")}</span>
                  <span className="text-sm">{tCommon("guests", { count: guests })}</span>
                </span>
                <ChevronDown className="size-4 text-muted-foreground" aria-hidden="true" />
              </button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-80">
              <Stepper
                id="booking-guests"
                label={t("guests")}
                description={t("maxGuests", { count: maxGuests })}
                value={guests}
                max={maxGuests}
                onChange={onGuestsChange}
              />
            </PopoverContent>
          </Popover>
        </div>

        {minNights > 1 && !lengthIssue && (
          <p className="-mt-2 flex items-center gap-1.5 text-xs text-muted-foreground">
            <Info className="size-3.5 shrink-0" aria-hidden="true" />
            {t("minNights", { count: minNights })}
          </p>
        )}
        {conflict && (
          <p role="alert" className="flex items-start gap-2 text-sm text-destructive">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {t("conflict")}
          </p>
        )}
        {!conflict && lengthIssue && (
          <p role="alert" className="flex items-start gap-2 text-sm text-destructive">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {lengthIssue === "tooShort"
              ? t("tooShort", { count: minNights })
              : t("tooLong", { count: maxNights })}
          </p>
        )}
        {bookedDatesUnavailable && <p className="text-xs text-muted-foreground">{t("availabilityUnavailable")}</p>}

        {isOwnListing ? (
          <p className="rounded-xl bg-muted/70 p-3 text-center text-sm text-muted-foreground">{t("ownListing")}</p>
        ) : (
          <Button size="lg" className="w-full" onClick={onReserve}>
            {validStay ? ctaLabel : t("checkAvailability")}
          </Button>
        )}

        <div aria-live="polite" className="grid gap-3">
          {validStay ? (
            priceQuery.isError ? (
              <p className="flex items-start gap-2 text-sm text-destructive">
                <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
                {errorMessage(priceQuery.error)}
              </p>
            ) : priceQuery.data ? (
              <>
                <p className="text-center text-xs text-muted-foreground">
                  {apartment.instantBook ? t("notChargedYet") : t("requestNotCharged")}
                </p>
                <PriceBreakdown
                  price={priceQuery.data}
                  nights={nights}
                  className={cn(priceQuery.isPlaceholderData && "opacity-60 transition-opacity")}
                />
                <ChargedInNote currency={priceQuery.data.currency} className="text-center" />
              </>
            ) : (
              <div className="grid gap-3" role="status" aria-label={t("calculating")}>
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-5/6" />
                <Skeleton className="h-5 w-full" />
              </div>
            )
          ) : (
            <p className="text-center text-sm text-muted-foreground">{t("addDatesForTotal")}</p>
          )}
          {(apartment.weeklyDiscountPercent > 0 || apartment.monthlyDiscountPercent > 0) && !priceQuery.data?.discountAmount && (
            <p className="text-center text-xs text-success">
              {apartment.weeklyDiscountPercent > 0
                ? t("weeklyDiscountHint", { percent: String(apartment.weeklyDiscountPercent) })
                : t("monthlyDiscountHint", { percent: String(apartment.monthlyDiscountPercent) })}
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
                <Money amount={priceQuery.data.totalPrice} currency={priceQuery.data.currency} className="font-semibold" />
                <span className="text-xs text-muted-foreground">
                  {formatShortDate(validStay.from, locale)} – {formatShortDate(validStay.to, locale)}
                </span>
              </>
            ) : (
              <>
                <span className="font-semibold">
                  <Money amount={apartment.price} currency={apartment.currency} compact />{" "}
                  <span className="font-normal text-muted-foreground">{tCommon("perNight")}</span>
                </span>
                <span className="text-xs text-muted-foreground">{t("addDatesShort")}</span>
              </>
            )}
          </div>
          {!isOwnListing && (
            <Button onClick={onReserve} disabled={Boolean(validStay) && priceQuery.isFetching && !priceQuery.data}>
              {validStay && priceQuery.isFetching && !priceQuery.data && <Loader2 className="animate-spin" />}
              {validStay ? ctaLabel : t("checkAvailability")}
            </Button>
          )}
        </div>
      </div>
    </>
  );
}

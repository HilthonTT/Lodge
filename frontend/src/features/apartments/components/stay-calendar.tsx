"use client";

import { createContext, use, useMemo } from "react";
import type { DateRange, DayButtonProps, Matcher } from "react-day-picker";

import { Calendar } from "@/components/ui/calendar";
import { useDisplayMoney } from "@/features/payments/currency-provider";
import { addDays, earliestCheckIn, nightsBetween, parseDateOnly, type StayRange, toDateOnly, today } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { CalendarDay } from "@/types/api";

type StayCalendarProps = {
  selected: DateRange | undefined;
  onSelect: (range: DateRange | undefined) => void;
  /** Nightly prices and availability from GET /apartments/{id}/calendar, keyed by yyyy-MM-dd. */
  days: Map<string, CalendarDay> | undefined;
  /** Unavailable nights from GET /apartments/{id}/booked-dates (used until the calendar loads). */
  bookedDates: string[];
  currency: string;
  minNights: number;
  maxNights: number;
  /** Nights to treat as free, e.g. the booking being moved. */
  ownStay?: StayRange;
  numberOfMonths?: number;
  autoFocus?: boolean;
};

/**
 * Range picker for a stay: shows each night's price, crosses out unavailable
 * nights and, once check-in is picked, only allows check-outs that respect the
 * listing's minimum and maximum number of nights. A check-out may fall on an
 * unavailable night (the next guest arrives that afternoon).
 */
export function StayCalendar({
  selected,
  onSelect,
  days,
  bookedDates,
  currency,
  minNights,
  maxNights,
  ownStay,
  numberOfMonths = 1,
  autoFocus,
}: StayCalendarProps) {
  const unavailable = useMemo(() => {
    const own = new Set<string>();
    if (ownStay) {
      for (let d = new Date(ownStay.from); d < ownStay.to; d = addDays(d, 1)) own.add(toDateOnly(d));
    }
    const keys = new Set<string>();
    for (const value of bookedDates) {
      const date = parseDateOnly(value);
      if (date) keys.add(toDateOnly(date));
    }
    if (days) for (const day of days.values()) if (!day.available) keys.add(day.date);
    for (const key of own) keys.delete(key);
    return [...keys]
      .map((key) => parseDateOnly(key)!)
      .sort((a, b) => a.getTime() - b.getTime());
  }, [bookedDates, days, ownStay]);

  const disabled = useMemo<Matcher[]>(() => {
    const past = { before: earliestCheckIn() };
    // Picking check-out: only check-in is set (react-day-picker may report it as a 0-night range).
    if (selected?.from && (!selected.to || nightsBetween(selected.from, selected.to) === 0)) {
      const from = selected.from;
      const nextUnavailable = unavailable.find((d) => d > from);
      const latest = addDays(from, Math.max(1, maxNights));
      const limit = nextUnavailable && nextUnavailable < latest ? nextUnavailable : latest;
      const earliest = addDays(from, Math.max(1, minNights));
      return [
        past,
        ...unavailable.filter((d) => d < from),
        { after: limit },
        // Too short: allow re-picking check-in by keeping `from` itself enabled.
        (date: Date) => date > from && date < earliest,
      ];
    }
    return [past, ...unavailable];
  }, [selected, unavailable, minNights, maxNights]);

  return (
    <PriceContext value={{ days, currency }}>
      <Calendar
        mode="range"
        numberOfMonths={numberOfMonths}
        selected={selected}
        // With a full range picked, the next click starts a new stay instead of stretching the old one.
        onSelect={(next, day) =>
          onSelect(
            selected?.from && selected.to && nightsBetween(selected.from, selected.to) > 0 ? { from: day, to: undefined } : next,
          )
        }
        disabled={disabled}
        modifiers={{ booked: unavailable }}
        modifiersClassNames={{ booked: "[&>button]:line-through" }}
        startMonth={today()}
        endMonth={addDays(today(), 365)}
        defaultMonth={selected?.from ?? today()}
        autoFocus={autoFocus}
        className="[--cell-size:2.75rem]"
        components={{ DayButton: PricedDayButton }}
      />
    </PriceContext>
  );
}

const PriceContext = createContext<{ days: Map<string, CalendarDay> | undefined; currency: string }>({
  days: undefined,
  currency: "USD",
});

/** Day cell with the night's price underneath. */
function PricedDayButton({ day, modifiers, className, children, ...rest }: DayButtonProps) {
  const { days, currency } = use(PriceContext);
  const money = useDisplayMoney();
  const info = days?.get(toDateOnly(day.date));
  const showPrice = info && info.available && !modifiers.disabled && !modifiers.outside;
  return (
    <button className={cn(className, "flex-col gap-0 leading-none")} {...rest}>
      <span>{children}</span>
      {showPrice && (
        <span className="mt-0.5 text-[0.6rem] font-normal tabular-nums opacity-70">
          {money.format(info.price, currency, true)}
        </span>
      )}
    </button>
  );
}

/** Stay length checks shared by the booking panel and the change-dates dialog. */
export function stayLengthIssue(range: StayRange | undefined, minNights: number, maxNights: number) {
  if (!range) return undefined;
  const nights = nightsBetween(range.from, range.to);
  if (nights < minNights) return "tooShort" as const;
  if (maxNights > 0 && nights > maxNights) return "tooLong" as const;
  return undefined;
}

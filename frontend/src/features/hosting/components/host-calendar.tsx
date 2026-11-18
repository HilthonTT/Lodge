"use client";

import {
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameMonth,
  startOfMonth,
  startOfWeek,
} from "date-fns";
import { Ban, ChevronLeft, ChevronRight, CircleCheck, Loader2, RotateCcw, Tag, X } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, formatMonthYear, getDateFnsLocale, today, toDateOnly } from "@/lib/dates";
import { formatPrice, formatPriceCompact } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { HostCalendarDay } from "@/types/api";

import { useHostCalendar, useHostListing, useUpdateCalendar } from "../hooks/use-hosting";
import { HostingError } from "./hosting-gate";

type Selection = { anchor: Date; end: Date };

const ordered = (a: Date, b: Date) => (a <= b ? ([a, b] as const) : ([b, a] as const));

export function HostCalendar({ listingId }: { listingId: string }) {
  const t = useTranslations("hosting.calendar");
  const locale = useLocale();
  const dfLocale = getDateFnsLocale(locale);
  const listing = useHostListing(listingId);
  const [month, setMonth] = useState(() => startOfMonth(today()));
  const [selection, setSelection] = useState<Selection | null>(null);
  const dragging = useRef(false);

  const from = toDateOnly(startOfMonth(month));
  const to = toDateOnly(endOfMonth(month));
  const calendar = useHostCalendar(listingId, from, to);

  const byDate = useMemo(() => new Map((calendar.data ?? []).map((d) => [d.date, d])), [calendar.data]);
  const days = useMemo(
    () =>
      eachDayOfInterval({
        start: startOfWeek(startOfMonth(month), { locale: dfLocale }),
        end: endOfWeek(endOfMonth(month), { locale: dfLocale }),
      }),
    [month, dfLocale],
  );
  const weekdays = days.slice(0, 7).map((d) => format(d, "EEEEEE", { locale: dfLocale }));
  const firstDay = today();

  // Finish a drag even when the pointer is released outside the grid.
  useEffect(() => {
    const stop = () => (dragging.current = false);
    window.addEventListener("pointerup", stop);
    return () => window.removeEventListener("pointerup", stop);
  }, []);

  const range = selection ? ordered(selection.anchor, selection.end) : null;
  const isSelected = (day: Date) => Boolean(range && day >= range[0] && day <= range[1]);

  const onPointerDown = (day: Date, event: React.PointerEvent) => {
    if (event.button !== 0) return;
    dragging.current = true;
    if (event.shiftKey && selection) {
      setSelection({ anchor: selection.anchor, end: day });
    } else if (selection && selection.anchor.getTime() === selection.end.getTime()) {
      // Second click extends a single-night selection into a range.
      setSelection({ anchor: selection.anchor, end: day });
    } else {
      setSelection({ anchor: day, end: day });
    }
  };

  const onPointerEnter = (day: Date) => {
    if (dragging.current && selection) setSelection({ anchor: selection.anchor, end: day });
  };

  const onKeyDown = (day: Date, event: React.KeyboardEvent) => {
    if (event.key !== "Enter" && event.key !== " ") return;
    event.preventDefault();
    if ((event.shiftKey || (selection && selection.anchor.getTime() === selection.end.getTime())) && selection) {
      setSelection({ anchor: selection.anchor, end: day });
    } else {
      setSelection({ anchor: day, end: day });
    }
  };

  const selectedDays = range
    ? (calendar.data ?? []).filter((d) => d.date >= toDateOnly(range[0]) && d.date <= toDateOnly(range[1]))
    : [];
  const currency = listing.data?.currency ?? "USD";

  return (
    <div className="grid gap-8">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="grid gap-1">
          <p className="text-sm text-muted-foreground">
            <Link href="/hosting/listings" className="underline-offset-4 hover:underline">
              {t("breadcrumb")}
            </Link>
          </p>
          <h1 className="text-2xl font-semibold sm:text-3xl">
            {listing.data ? listing.data.name : <Skeleton className="h-8 w-64" />}
          </h1>
          {listing.data && (
            <p className="text-sm text-muted-foreground">
              {t("basePrice", { price: formatPrice(listing.data.price, currency, locale) })}
              {listing.data.weekendPrice !== null &&
                ` · ${t("weekendPrice", { price: formatPrice(listing.data.weekendPrice, currency, locale) })}`}
            </p>
          )}
        </div>
        <Legend />
      </header>

      {listing.isError ? (
        <HostingError error={listing.error} onRetry={() => listing.refetch()} />
      ) : (
        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_20rem] lg:items-start">
          <section aria-labelledby="month-heading" className="grid gap-4 rounded-2xl border bg-card p-4 shadow-card sm:p-6">
            <div className="flex items-center justify-between gap-2">
              <Button
                variant="ghost"
                size="icon"
                className="rounded-full"
                onClick={() => setMonth((m) => addMonths(m, -1))}
                disabled={month <= startOfMonth(firstDay)}
                aria-label={t("previousMonth")}
              >
                <ChevronLeft aria-hidden="true" />
              </Button>
              <h2 id="month-heading" className="text-lg font-semibold capitalize" aria-live="polite">
                {formatMonthYear(month, locale)}
              </h2>
              <Button
                variant="ghost"
                size="icon"
                className="rounded-full"
                onClick={() => setMonth((m) => addMonths(m, 1))}
                disabled={month >= addMonths(startOfMonth(firstDay), 11)}
                aria-label={t("nextMonth")}
              >
                <ChevronRight aria-hidden="true" />
              </Button>
            </div>

            {calendar.isError ? (
              <HostingError error={calendar.error} onRetry={() => calendar.refetch()} />
            ) : (
              <div
                role="grid"
                aria-label={t("gridLabel")}
                aria-multiselectable="true"
                className={cn("grid touch-none gap-1 select-none", calendar.isFetching && "opacity-70 transition-opacity")}
              >
                <div role="row" className="grid grid-cols-7 gap-1">
                  {weekdays.map((w, i) => (
                    <span
                      key={i}
                      role="columnheader"
                      className="pb-1 text-center text-xs font-medium text-muted-foreground uppercase"
                    >
                      {w}
                    </span>
                  ))}
                </div>
                {Array.from({ length: days.length / 7 }, (_, week) => (
                  <div role="row" key={week} className="grid grid-cols-7 gap-1">
                    {days.slice(week * 7, week * 7 + 7).map((day) => {
                      const key = toDateOnly(day);
                      const info = byDate.get(key);
                      const inMonth = isSameMonth(day, month);
                      const past = day < firstDay;
                      if (!inMonth) return <span key={key} role="gridcell" aria-hidden="true" />;
                      return (
                        <DayCell
                          key={key}
                          day={day}
                          info={info}
                          currency={currency}
                          past={past}
                          selected={isSelected(day)}
                          loading={calendar.isPending}
                          onPointerDown={(e) => onPointerDown(day, e)}
                          onPointerEnter={() => onPointerEnter(day)}
                          onKeyDown={(e) => onKeyDown(day, e)}
                        />
                      );
                    })}
                  </div>
                ))}
              </div>
            )}
            <p className="text-xs text-muted-foreground">{t("hint")}</p>
          </section>

          <SelectionPanel
            listingId={listingId}
            range={range}
            days={selectedDays}
            currency={currency}
            basePrice={listing.data?.price}
            onClear={() => setSelection(null)}
          />
        </div>
      )}
    </div>
  );
}

function DayCell({
  day,
  info,
  currency,
  past,
  selected,
  loading,
  onPointerDown,
  onPointerEnter,
  onKeyDown,
}: {
  day: Date;
  info: HostCalendarDay | undefined;
  currency: string;
  past: boolean;
  selected: boolean;
  loading: boolean;
  onPointerDown: (e: React.PointerEvent) => void;
  onPointerEnter: () => void;
  onKeyDown: (e: React.KeyboardEvent) => void;
}) {
  const t = useTranslations("hosting.calendar");
  const locale = useLocale();
  const state = info?.booked ? "booked" : info?.blocked ? "blocked" : "open";
  const label = [
    format(day, "PPPP", { locale: getDateFnsLocale(locale) }),
    info ? formatPrice(info.price, currency, locale) : "",
    state === "booked" ? t("legend.booked") : state === "blocked" ? t("legend.blocked") : "",
    info?.customPrice ? t("legend.custom") : "",
  ]
    .filter(Boolean)
    .join(", ");

  return (
    <button
      type="button"
      role="gridcell"
      aria-selected={selected}
      aria-label={label}
      disabled={past}
      data-date={toDateOnly(day)}
      onPointerDown={onPointerDown}
      onPointerEnter={onPointerEnter}
      onKeyDown={onKeyDown}
      className={cn(
        "relative flex aspect-square min-h-14 flex-col justify-between rounded-xl border p-1.5 text-left transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40 sm:min-h-20 sm:p-2",
        "disabled:cursor-not-allowed disabled:opacity-40",
        state === "booked" && "border-primary/30 bg-primary/10",
        state === "blocked" &&
          "bg-[repeating-linear-gradient(135deg,var(--muted)_0_6px,transparent_6px_12px)] text-muted-foreground",
        state === "open" && "bg-background hover:bg-muted/60",
        selected && "border-foreground bg-foreground/[0.06] ring-2 ring-foreground",
      )}
    >
      <span className={cn("text-sm font-semibold", state === "blocked" && "line-through")}>{day.getDate()}</span>
      {loading ? (
        <Skeleton className="h-3 w-3/4" />
      ) : info ? (
        <span
          className={cn(
            "truncate text-[0.7rem] tabular-nums sm:text-xs",
            info.customPrice ? "font-semibold text-primary" : "text-muted-foreground",
          )}
        >
          {state === "booked" ? t("bookedShort") : formatPriceCompact(info.price, currency, locale)}
        </span>
      ) : null}
      {info?.customPrice && (
        <span className="absolute top-1.5 right-1.5 size-1.5 rounded-full bg-primary" aria-hidden="true" />
      )}
    </button>
  );
}

function Legend() {
  const t = useTranslations("hosting.calendar.legend");
  const item = "inline-flex items-center gap-1.5";
  return (
    <ul className="flex flex-wrap gap-x-4 gap-y-1.5 text-xs text-muted-foreground">
      <li className={item}>
        <span className="size-3 rounded border bg-background" aria-hidden="true" /> {t("available")}
      </li>
      <li className={item}>
        <span className="size-3 rounded border border-primary/30 bg-primary/10" aria-hidden="true" /> {t("booked")}
      </li>
      <li className={item}>
        <span
          className="size-3 rounded border bg-[repeating-linear-gradient(135deg,var(--muted-foreground)_0_2px,transparent_2px_4px)]"
          aria-hidden="true"
        />{" "}
        {t("blocked")}
      </li>
      <li className={item}>
        <span className="size-2 rounded-full bg-primary" aria-hidden="true" /> {t("custom")}
      </li>
    </ul>
  );
}

function SelectionPanel({
  listingId,
  range,
  days,
  currency,
  basePrice,
  onClear,
}: {
  listingId: string;
  range: readonly [Date, Date] | null;
  days: HostCalendarDay[];
  currency: string;
  basePrice: number | undefined;
  onClear: () => void;
}) {
  const t = useTranslations("hosting.calendar");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const update = useUpdateCalendar(listingId);
  const [price, setPrice] = useState("");
  const [priceError, setPriceError] = useState<string>();

  if (!range) {
    return (
      <aside className="grid gap-2 rounded-2xl border border-dashed p-6 text-center text-sm text-muted-foreground lg:sticky lg:top-28">
        <p className="font-medium text-foreground">{t("noSelectionTitle")}</p>
        <p>{t("noSelectionDescription")}</p>
      </aside>
    );
  }

  const [start, end] = range;
  const nights = days.length || Math.round((end.getTime() - start.getTime()) / 86_400_000) + 1;
  const anyBlocked = days.some((d) => d.blocked);
  const anyOpen = days.some((d) => !d.blocked && !d.booked);
  const anyCustom = days.some((d) => d.customPrice);
  const anyBooked = days.some((d) => d.booked);
  const body = { from: toDateOnly(start), to: toDateOnly(end) };

  const run = (patch: Parameters<typeof update.mutate>[0], success: string) =>
    update.mutate(patch, {
      onSuccess: () => {
        toast.success(success, { description: formatDateRange(start, end, locale) });
        setPrice("");
      },
      onError: (error) => toast.error(t("updateError"), { description: errorMessage(error) }),
    });

  const onSetPrice = (event: React.FormEvent) => {
    event.preventDefault();
    const value = Number(price.replace(",", "."));
    if (!price || !Number.isFinite(value) || value <= 0) {
      setPriceError(t("priceInvalid"));
      return;
    }
    run({ ...body, price: Math.round(value * 100) / 100 }, t("priceSet"));
  };

  return (
    <aside
      aria-label={t("selectionLabel")}
      className="grid gap-5 rounded-2xl border bg-card p-6 shadow-float lg:sticky lg:top-28"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="grid gap-0.5">
          <p className="font-semibold">{formatDateRange(start, end, locale)}</p>
          <p className="text-sm text-muted-foreground">{tCommon("nights", { count: nights })}</p>
        </div>
        <Button variant="ghost" size="icon-sm" onClick={onClear} aria-label={t("clearSelection")}>
          <X aria-hidden="true" />
        </Button>
      </div>
      {anyBooked && (
        <Badge variant="info" className="whitespace-normal">
          {t("includesBooked")}
        </Badge>
      )}

      <div className="grid gap-2">
        <p className="text-sm font-medium">{t("availability")}</p>
        <div className="grid grid-cols-2 gap-2">
          <Button
            variant="outline"
            onClick={() => run({ ...body, blocked: true }, t("blockedToast"))}
            disabled={update.isPending || !anyOpen}
          >
            <Ban aria-hidden="true" /> {t("block")}
          </Button>
          <Button
            variant="outline"
            onClick={() => run({ ...body, blocked: false }, t("unblockedToast"))}
            disabled={update.isPending || !anyBlocked}
          >
            <CircleCheck aria-hidden="true" /> {t("unblock")}
          </Button>
        </div>
      </div>

      <form onSubmit={onSetPrice} className="grid gap-2" noValidate>
        <Label htmlFor="calendar-price">{t("nightlyPrice", { currency })}</Label>
        <div className="flex gap-2">
          <Input
            id="calendar-price"
            inputMode="decimal"
            value={price}
            onChange={(e) => {
              setPrice(e.target.value);
              setPriceError(undefined);
            }}
            placeholder={basePrice !== undefined ? String(basePrice) : ""}
            aria-invalid={Boolean(priceError)}
            aria-describedby={priceError ? "calendar-price-error" : undefined}
          />
          <Button type="submit" disabled={update.isPending}>
            {update.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Tag aria-hidden="true" />}
            {t("setPrice")}
          </Button>
        </div>
        {priceError && (
          <p id="calendar-price-error" className="text-xs font-medium text-destructive">
            {priceError}
          </p>
        )}
        <Button
          type="button"
          variant="ghost"
          className="justify-self-start px-2"
          onClick={() => run({ ...body, clearPrice: true }, t("priceCleared"))}
          disabled={update.isPending || !anyCustom}
        >
          <RotateCcw aria-hidden="true" /> {t("clearPrice")}
        </Button>
      </form>
    </aside>
  );
}

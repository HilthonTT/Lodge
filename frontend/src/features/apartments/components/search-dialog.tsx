"use client";

import { format } from "date-fns";
import { CalendarDays, MapPin, Search, Users } from "lucide-react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import type { DateRange } from "react-day-picker";

import { Stepper } from "@/components/shared/stepper";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { earliestCheckIn, formatDateRange, getDateFnsLocale, nightsBetween, today } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { FlexibleStay } from "@/types/api";

import { formatMonthList, monthToDate, upcomingMonths } from "../flexible";
import {
  type ApartmentSearch,
  buildUrl,
  defaultFlexibleMonths,
  FLEXIBLE_STAYS,
  MAX_GUESTS,
  toSearchParams,
} from "../search-params";
import { LocationAutocomplete } from "./location-autocomplete";

export type SearchSection = "where" | "when" | "who";

type SearchDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initial: ApartmentSearch;
  focus: SearchSection;
};

export function SearchDialog({ open, onOpenChange, initial, focus }: SearchDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="gap-0 p-0 sm:max-w-[44rem]">
        {/* Remount the form each time it opens so it reflects the current URL. */}
        {open && <SearchForm initial={initial} focus={focus} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

type DateMode = "dates" | "flexible";

function SearchForm({
  initial,
  focus,
  onDone,
}: {
  initial: ApartmentSearch;
  focus: SearchSection;
  onDone: () => void;
}) {
  const t = useTranslations("search.dialog");
  const tSearch = useTranslations("search");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const [where, setWhere] = useState(initial.q ?? "");
  const [mode, setMode] = useState<DateMode>(initial.flexible ? "flexible" : "dates");
  const [range, setRange] = useState<DateRange | undefined>(initial.range);
  const [flexible, setFlexible] = useState<FlexibleStay>(initial.flexible ?? "weekend");
  const [months, setMonths] = useState<string[]>(initial.months.length ? initial.months : defaultFlexibleMonths().slice(0, 2));
  const [guests, setGuests] = useState(initial.guests ?? 1);
  const monthOptions = upcomingMonths(12);

  const validRange =
    range?.from && range.to && nightsBetween(range.from, range.to) > 0
      ? { from: range.from, to: range.to }
      : undefined;

  const toggleMonth = (month: string) =>
    setMonths((current) =>
      current.includes(month) ? current.filter((m) => m !== month) : [...current, month].sort(),
    );

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const isFlexible = mode === "flexible";
    const params = toSearchParams({
      ...initial,
      q: where.trim() || undefined,
      range: isFlexible ? undefined : validRange,
      flexible: isFlexible ? flexible : undefined,
      months: isFlexible ? (months.length ? months : defaultFlexibleMonths()) : [],
      guests,
      // A new destination makes the previous map area meaningless.
      bounds: undefined,
      page: 1,
    });
    router.push(buildUrl("/search", params));
    onDone();
  };

  const onClear = () => {
    setWhere("");
    setRange(undefined);
    setMode("dates");
    setMonths(defaultFlexibleMonths().slice(0, 2));
    setGuests(1);
  };

  const whenSummary =
    mode === "flexible"
      ? tSearch("flexibleIn", { kind: flexible, months: formatMonthList(months.length ? months : defaultFlexibleMonths(), locale) })
      : validRange
        ? `${formatDateRange(validRange.from, validRange.to, locale)} · ${tCommon("nights", { count: nightsBetween(validRange.from, validRange.to) })}`
        : range?.from
          ? t("selectCheckOut")
          : t("anyWeek");

  const chip =
    "inline-flex h-10 items-center justify-center rounded-full border px-4 text-sm font-medium transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40";

  return (
    <form onSubmit={onSubmit} className="flex flex-col">
      <DialogHeader className="px-6 pt-6 pb-4">
        <DialogTitle>{t("title")}</DialogTitle>
        <DialogDescription>{t("description")}</DialogDescription>
      </DialogHeader>

      <div className="grid gap-6 px-6 pb-6">
        <section className="grid gap-2">
          <Label htmlFor="search-where" className="gap-1.5">
            <MapPin className="size-4 text-muted-foreground" aria-hidden="true" /> {t("where")}
          </Label>
          <LocationAutocomplete
            id="search-where"
            value={where}
            onChange={setWhere}
            placeholder={t("wherePlaceholder")}
            autoFocus={focus === "where"}
          />
        </section>

        <Separator />

        <section className="grid gap-4" aria-labelledby="search-when-label">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span id="search-when-label" className="inline-flex items-center gap-1.5 text-sm font-medium">
              <CalendarDays className="size-4 text-muted-foreground" aria-hidden="true" /> {t("when")}
            </span>
            <span className="text-sm text-muted-foreground" aria-live="polite">
              {whenSummary}
            </span>
          </div>
          <Tabs value={mode} onValueChange={(v) => setMode(v as DateMode)} className="grid gap-5">
            <TabsList className="mx-auto">
              <TabsTrigger value="dates">{t("tabDates")}</TabsTrigger>
              <TabsTrigger value="flexible">{t("tabFlexible")}</TabsTrigger>
            </TabsList>
            <TabsContent value="dates" className="flex justify-center">
              <Calendar
                mode="range"
                numberOfMonths={2}
                selected={range}
                onSelect={setRange}
                disabled={{ before: earliestCheckIn() }}
                startMonth={today()}
                autoFocus={focus === "when"}
              />
            </TabsContent>
            <TabsContent value="flexible" className="grid gap-6">
              <fieldset className="grid gap-3">
                <legend className="mb-3 text-center text-base font-semibold">{t("stayFor")}</legend>
                <div className="flex flex-wrap justify-center gap-2">
                  {FLEXIBLE_STAYS.map((kind) => (
                    <button
                      key={kind}
                      type="button"
                      aria-pressed={flexible === kind}
                      onClick={() => setFlexible(kind)}
                      className={cn(
                        chip,
                        flexible === kind ? "border-foreground bg-foreground text-background" : "bg-card hover:border-foreground/60",
                      )}
                    >
                      {t(`stay.${kind}`)}
                    </button>
                  ))}
                </div>
              </fieldset>
              <fieldset className="grid gap-3">
                <legend className="mb-3 text-center text-base font-semibold">{t("goIn")}</legend>
                <div className="-mx-1 flex snap-x gap-3 overflow-x-auto px-1 pb-2">
                  {monthOptions.map((month) => {
                    const date = monthToDate(month);
                    const selected = months.includes(month);
                    return (
                      <button
                        key={month}
                        type="button"
                        aria-pressed={selected}
                        onClick={() => toggleMonth(month)}
                        className={cn(
                          "grid w-24 shrink-0 snap-start justify-items-center gap-1 rounded-2xl border px-3 py-4 text-sm transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40",
                          selected ? "border-foreground bg-muted ring-1 ring-foreground" : "bg-card hover:border-foreground/60",
                        )}
                      >
                        <CalendarDays className="size-6" aria-hidden="true" />
                        <span className="font-medium capitalize">
                          {format(date, "LLLL", { locale: getDateFnsLocale(locale) })}
                        </span>
                        <span className="text-xs text-muted-foreground">{String(date.getFullYear())}</span>
                      </button>
                    );
                  })}
                </div>
              </fieldset>
            </TabsContent>
          </Tabs>
        </section>

        <Separator />

        <section className="grid gap-4" aria-label={t("who")}>
          <span className="inline-flex items-center gap-1.5 text-sm font-medium">
            <Users className="size-4 text-muted-foreground" aria-hidden="true" /> {t("who")}
          </span>
          <Stepper
            id="search-guests"
            label={t("guests")}
            description={t("guestsDescription")}
            value={guests}
            max={MAX_GUESTS}
            onChange={setGuests}
          />
        </section>
      </div>

      <div className="sticky bottom-0 flex items-center justify-between gap-3 border-t bg-popover px-6 py-4">
        <Button type="button" variant="link" className="px-0 text-foreground underline" onClick={onClear}>
          {t("clearAll")}
        </Button>
        <Button type="submit" size="lg" className="rounded-full px-7">
          <Search aria-hidden="true" /> {t("submit")}
        </Button>
      </div>
    </form>
  );
}

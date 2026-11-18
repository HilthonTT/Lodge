"use client";

import { CalendarDays, MapPin, Search, Users } from "lucide-react";
import { useRouter } from "next/navigation";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { formatDateRange, nightsBetween, today } from "@/lib/dates";
import { pluralize } from "@/lib/format";

import {
  type ApartmentSearch,
  buildUrl,
  MAX_GUESTS,
  MAX_ROOMS,
  toSearchParams,
} from "../search-params";

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

function SearchForm({
  initial,
  focus,
  onDone,
}: {
  initial: ApartmentSearch;
  focus: SearchSection;
  onDone: () => void;
}) {
  const router = useRouter();
  const [where, setWhere] = useState(initial.q ?? "");
  const [range, setRange] = useState<DateRange | undefined>(initial.range);
  const [guests, setGuests] = useState(initial.guests ?? 1);
  const [rooms, setRooms] = useState(initial.rooms ?? 1);

  const validRange =
    range?.from && range.to && nightsBetween(range.from, range.to) > 0
      ? { from: range.from, to: range.to }
      : undefined;

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const params = toSearchParams({
      q: where.trim() || undefined,
      range: validRange,
      guests,
      rooms,
      sort: initial.sort,
    });
    router.push(buildUrl("/search", params));
    onDone();
  };

  const onClear = () => {
    setWhere("");
    setRange(undefined);
    setGuests(1);
    setRooms(1);
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col">
      <DialogHeader className="px-6 pt-6 pb-4">
        <DialogTitle>Find a stay</DialogTitle>
        <DialogDescription>Search by destination, dates and party size.</DialogDescription>
      </DialogHeader>

      <div className="grid gap-6 px-6 pb-6">
        <section className="grid gap-2">
          <Label htmlFor="search-where" className="gap-1.5">
            <MapPin className="size-4 text-muted-foreground" aria-hidden="true" /> Where
          </Label>
          <Input
            id="search-where"
            value={where}
            onChange={(e) => setWhere(e.target.value)}
            placeholder="Country, city or stay name"
            autoComplete="off"
            autoFocus={focus === "where"}
            maxLength={100}
          />
        </section>

        <Separator />

        <section className="grid gap-3" aria-labelledby="search-when-label">
          <div className="flex items-center justify-between gap-2">
            <span id="search-when-label" className="inline-flex items-center gap-1.5 text-sm font-medium">
              <CalendarDays className="size-4 text-muted-foreground" aria-hidden="true" /> When
            </span>
            <span className="text-sm text-muted-foreground" aria-live="polite">
              {validRange
                ? `${formatDateRange(validRange.from, validRange.to)} · ${pluralize(nightsBetween(validRange.from, validRange.to), "night")}`
                : range?.from
                  ? "Select a check-out date"
                  : "Any week"}
            </span>
          </div>
          <div className="flex justify-center">
            <Calendar
              mode="range"
              numberOfMonths={2}
              selected={range}
              onSelect={setRange}
              disabled={{ before: today() }}
              startMonth={today()}
              autoFocus={focus === "when"}
            />
          </div>
        </section>

        <Separator />

        <section className="grid gap-4" aria-label="Who">
          <span className="inline-flex items-center gap-1.5 text-sm font-medium">
            <Users className="size-4 text-muted-foreground" aria-hidden="true" /> Who
          </span>
          <Stepper
            id="search-guests"
            label="Guests"
            description="Number of people staying"
            value={guests}
            max={MAX_GUESTS}
            onChange={setGuests}
          />
          <Stepper
            id="search-rooms"
            label="Rooms"
            description="Minimum number of rooms"
            value={rooms}
            max={MAX_ROOMS}
            onChange={setRooms}
          />
        </section>
      </div>

      <div className="sticky bottom-0 flex items-center justify-between gap-3 border-t bg-popover px-6 py-4">
        <Button type="button" variant="link" className="px-0 text-foreground underline" onClick={onClear}>
          Clear all
        </Button>
        <Button type="submit" size="lg" className="rounded-full px-7">
          <Search aria-hidden="true" /> Search
        </Button>
      </div>
    </form>
  );
}

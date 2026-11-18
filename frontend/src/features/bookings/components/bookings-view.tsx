"use client";

import { CalendarX2 } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";
import { type Booking, BookingStatus } from "@/types/api";

import { useUserBookings } from "../hooks/use-bookings";
import { BookingCard } from "./booking-card";

// `labelKey` lives under `bookings` in messages/*.json.
const FILTERS = [
  { key: "all", labelKey: "filterAll", statuses: null },
  { key: "pending", labelKey: "status.reserved", statuses: [BookingStatus.Reserved] },
  { key: "confirmed", labelKey: "status.confirmed", statuses: [BookingStatus.Confirmed] },
  { key: "completed", labelKey: "status.completed", statuses: [BookingStatus.Completed] },
  { key: "cancelled", labelKey: "status.cancelled", statuses: [BookingStatus.Cancelled, BookingStatus.Rejected] },
] as const;

type FilterKey = (typeof FILTERS)[number]["key"];

const ORDER: Record<BookingStatus, number> = {
  [BookingStatus.Reserved]: 0,
  [BookingStatus.Confirmed]: 1,
  [BookingStatus.Completed]: 2,
  [BookingStatus.Cancelled]: 3,
  [BookingStatus.Rejected]: 4,
};

function sortBookings(bookings: Booking[]) {
  return [...bookings].sort(
    (a, b) =>
      (ORDER[a.status] ?? 9) - (ORDER[b.status] ?? 9) ||
      a.durationStart.localeCompare(b.durationStart),
  );
}

export function BookingsView() {
  const t = useTranslations("bookings");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const { data, isPending, isError, error, refetch, isRefetching } = useUserBookings();
  const [filter, setFilter] = useState<FilterKey>("all");
  const highlight = useSearchParams().get("highlight");

  const counts = useMemo(() => {
    const result = {} as Record<FilterKey, number>;
    for (const f of FILTERS) {
      result[f.key] = f.statuses
        ? (data ?? []).filter((b) => (f.statuses as readonly BookingStatus[]).includes(b.status)).length
        : (data?.length ?? 0);
    }
    return result;
  }, [data]);

  const visible = useMemo(() => {
    const active = FILTERS.find((f) => f.key === filter);
    const list = data ?? [];
    return sortBookings(
      active?.statuses ? list.filter((b) => (active.statuses as readonly BookingStatus[]).includes(b.status)) : list,
    );
  }, [data, filter]);

  if (isPending) return <BookingsSkeleton />;

  if (isError) {
    return (
      <ErrorState
        title={t("loadError")}
        description={errorMessage(error)}
        action={
          <Button variant="outline" onClick={() => refetch()} disabled={isRefetching}>
            {tCommon("tryAgain")}
          </Button>
        }
      />
    );
  }

  if (data.length === 0) {
    return (
      <EmptyState
        icon={CalendarX2}
        title={t("emptyTitle")}
        description={t("emptyDescription")}
        action={
          <Button asChild>
            <Link href="/">{t("startExploring")}</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="grid gap-6">
      <div role="group" aria-label={t("filterLabel")} className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
        {FILTERS.map((f) => (
          <button
            key={f.key}
            type="button"
            aria-pressed={filter === f.key}
            onClick={() => setFilter(f.key)}
            className={cn(
              "inline-flex h-9 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
              filter === f.key
                ? "border-foreground bg-foreground text-background"
                : "bg-card hover:bg-muted",
            )}
          >
            {t(f.labelKey)}
            <span
              className={cn(
                "rounded-full px-1.5 text-xs tabular-nums",
                filter === f.key ? "bg-background/20" : "bg-muted text-muted-foreground",
              )}
            >
              {counts[f.key]}
            </span>
          </button>
        ))}
      </div>

      {visible.length === 0 ? (
        <EmptyState icon={CalendarX2} title={t("filterEmptyTitle")} description={t("filterEmptyDescription")} />
      ) : (
        <ul className="grid gap-4">
          {visible.map((booking, index) => (
            <li key={booking.id}>
              <BookingCard booking={booking} highlighted={booking.id === highlight} priority={index === 0} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function BookingsSkeleton() {
  const t = useTranslations("bookings");
  return (
    <div className="grid gap-6" role="status" aria-label={t("loading")}>
      <div className="flex gap-2">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-9 w-28 rounded-full" />
        ))}
      </div>
      {Array.from({ length: 3 }, (_, i) => (
        <div key={i} className="grid gap-4 rounded-2xl border p-4 sm:grid-cols-[11rem_1fr]">
          <Skeleton className="aspect-[4/3] rounded-xl sm:aspect-auto sm:h-32" />
          <div className="grid content-start gap-3">
            <Skeleton className="h-5 w-1/2" />
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-9 w-36" />
          </div>
        </div>
      ))}
    </div>
  );
}

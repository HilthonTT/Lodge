"use client";

import { CalendarX2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState } from "@/components/shared/state";
import { cn } from "@/lib/utils";

import { useHostBookings, useHostListings } from "../hooks/use-hosting";
import {
  RESERVATION_FILTERS,
  type ReservationFilter,
  RESERVATIONS_PAGE_SIZE,
  type ReservationsSearch,
  reservationsHref,
} from "../reservations-search-params";
import { HostBookingCard } from "./host-booking-card";
import { BookingsSkeleton } from "./hosting-dashboard";
import { HostingError } from "./hosting-gate";

export function ReservationsView({ search }: { search: ReservationsSearch }) {
  const t = useTranslations();
  const router = useRouter();
  const listings = useHostListings({ page: 1, pageSize: 100 });
  const bookings = useHostBookings({
    page: search.page,
    pageSize: RESERVATIONS_PAGE_SIZE,
    status: RESERVATION_FILTERS[search.status].status,
    listingId: search.listing,
  });

  const pages = bookings.data ? Math.ceil(bookings.data.totalCount / RESERVATIONS_PAGE_SIZE) : 0;
  const params: Record<string, string | undefined> = {
    status: search.status !== "all" ? search.status : undefined,
    listing: search.listing,
  };

  return (
    <div className="grid gap-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <nav aria-label={t("hosting.reservations.filterLabel")} className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
          {(Object.keys(RESERVATION_FILTERS) as ReservationFilter[]).map((key) => {
            const active = key === search.status;
            const labelKey = RESERVATION_FILTERS[key].labelKey;
            return (
              <Link
                key={key}
                href={reservationsHref({ ...search, status: key, page: 1 })}
                aria-current={active ? "page" : undefined}
                scroll={false}
                className={cn(
                  "inline-flex h-9 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                  active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
                )}
              >
                {labelKey ? t(`bookings.status.${labelKey}`) : t("hosting.reservations.all")}
                {active && bookings.data && (
                  <span className="rounded-full bg-background/20 px-1.5 text-xs tabular-nums">
                    {bookings.data.totalCount}
                  </span>
                )}
              </Link>
            );
          })}
        </nav>
        <div className="shrink-0">
          <label htmlFor="listing-filter" className="sr-only">
            {t("hosting.reservations.listingFilter")}
          </label>
          <select
            id="listing-filter"
            value={search.listing ?? ""}
            onChange={(e) =>
              router.push(reservationsHref({ ...search, listing: e.target.value || undefined, page: 1 }), {
                scroll: false,
              })
            }
            className="h-10 w-full max-w-xs appearance-none rounded-full border border-input bg-background px-4 text-sm font-medium shadow-xs outline-none focus-visible:ring-[3px] focus-visible:ring-ring/30 dark:bg-input/20"
          >
            <option value="">{t("hosting.reservations.allListings")}</option>
            {listings.data?.items.map((listing) => (
              <option key={listing.id} value={listing.id}>
                {listing.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {bookings.isPending ? (
        <BookingsSkeleton count={3} />
      ) : bookings.isError ? (
        <HostingError
          error={bookings.error}
          title={t("hosting.reservations.loadError")}
          onRetry={() => bookings.refetch()}
        />
      ) : bookings.data.items.length === 0 ? (
        <EmptyState
          icon={CalendarX2}
          title={t("hosting.reservations.emptyTitle")}
          description={t("hosting.reservations.emptyDescription")}
        />
      ) : (
        <>
          <ul className={cn("grid gap-4", bookings.isPlaceholderData && "opacity-60 transition-opacity")}>
            {bookings.data.items.map((booking) => (
              <li key={booking.id}>
                <HostBookingCard booking={booking} />
              </li>
            ))}
          </ul>
          <Pagination page={search.page} totalPages={pages} pathname="/hosting/reservations" searchParams={params} />
        </>
      )}
    </div>
  );
}

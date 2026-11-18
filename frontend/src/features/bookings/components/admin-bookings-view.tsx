"use client";

import { format } from "date-fns";
import { CalendarX2 } from "lucide-react";
import Link from "next/link";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { formatDateRange, nightsBetween, parseDateOnly } from "@/lib/dates";
import { formatPrice, pluralize } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { Booking } from "@/types/api";

import {
  ADMIN_PAGE_SIZE,
  ADMIN_STATUS_FILTERS,
  type AdminBookingsSearch,
  type AdminStatusFilter,
  toAdminSearchParams,
} from "../admin-search-params";
import { useAdminBookings } from "../hooks/use-admin-bookings";
import { AdminBookingActions } from "./admin-booking-actions";
import { BookingStatusBadge } from "./booking-status-badge";

const PATHNAME = "/admin/bookings";

function hrefFor(search: AdminBookingsSearch) {
  const query = new URLSearchParams(
    Object.entries(toAdminSearchParams(search)).filter((e): e is [string, string] => Boolean(e[1])),
  ).toString();
  return query ? `${PATHNAME}?${query}` : PATHNAME;
}

// Columns shared by the header row and every booking row (table layout from lg up).
const COLUMNS = "lg:grid-cols-[minmax(0,1.3fr)_minmax(0,1.6fr)_7rem_9.5rem_6.5rem_minmax(0,1.2fr)]";

export function AdminBookingsView({ search }: { search: AdminBookingsSearch }) {
  const status = ADMIN_STATUS_FILTERS[search.status].status;
  const { data, isPending, isError, error, refetch, isRefetching, isPlaceholderData } = useAdminBookings({
    page: search.page,
    pageSize: ADMIN_PAGE_SIZE,
    status,
  });

  const pages = data ? Math.ceil(data.totalCount / ADMIN_PAGE_SIZE) : 0;

  return (
    <div className="grid gap-6">
      <nav aria-label="Filter by status" className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
        {(Object.keys(ADMIN_STATUS_FILTERS) as AdminStatusFilter[]).map((key) => {
          const active = key === search.status;
          return (
            <Link
              key={key}
              href={hrefFor({ status: key, page: 1 })}
              aria-current={active ? "page" : undefined}
              scroll={false}
              className={cn(
                "inline-flex h-9 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
              )}
            >
              {ADMIN_STATUS_FILTERS[key].label}
              {active && data && (
                <span className="rounded-full bg-background/20 px-1.5 text-xs tabular-nums">{data.totalCount}</span>
              )}
            </Link>
          );
        })}
      </nav>

      {isPending ? (
        <AdminBookingsSkeleton rowsOnly />
      ) : isError ? (
        <ErrorState
          title="We couldn't load bookings"
          description={error.message}
          action={
            <Button variant="outline" onClick={() => refetch()} disabled={isRefetching}>
              Try again
            </Button>
          }
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          icon={CalendarX2}
          title={search.page > 1 ? "No bookings on this page" : "No bookings here"}
          description={
            search.status === "all"
              ? "Bookings from every guest will show up here."
              : `There are no ${ADMIN_STATUS_FILTERS[search.status].label.toLowerCase()} bookings right now.`
          }
          action={
            (search.page > 1 || search.status !== "all") && (
              <Button asChild variant="outline">
                <Link href={search.page > 1 ? hrefFor({ ...search, page: 1 }) : PATHNAME}>
                  {search.page > 1 ? "Back to first page" : "Show all bookings"}
                </Link>
              </Button>
            )
          }
        />
      ) : (
        <>
          <div
            className={cn("overflow-hidden rounded-2xl border bg-card shadow-card transition-opacity", {
              "opacity-60": isPlaceholderData,
            })}
            aria-busy={isPlaceholderData}
          >
            <div
              aria-hidden="true"
              className={cn(
                "hidden gap-4 border-b bg-muted/50 px-5 py-3 text-xs font-medium tracking-wide text-muted-foreground uppercase lg:grid",
                COLUMNS,
              )}
            >
              <span>Guest</span>
              <span>Stay</span>
              <span className="text-right">Total</span>
              <span>Status</span>
              <span>Created</span>
              <span>Actions</span>
            </div>
            <ul className="divide-y" aria-label="Bookings">
              {data.items.map((booking) => (
                <AdminBookingRow key={booking.id} booking={booking} />
              ))}
            </ul>
          </div>
          <p className="-mt-2 text-sm text-muted-foreground">
            Showing {(search.page - 1) * ADMIN_PAGE_SIZE + 1}–{(search.page - 1) * ADMIN_PAGE_SIZE + data.items.length}{" "}
            of {pluralize(data.totalCount, "booking")}
          </p>
          <Pagination
            page={search.page}
            totalPages={pages}
            pathname={PATHNAME}
            searchParams={toAdminSearchParams(search)}
          />
        </>
      )}
    </div>
  );
}

function AdminBookingRow({ booking }: { booking: Booking }) {
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const created = new Date(booking.createdOnUtc);

  return (
    <li className={cn("grid gap-3 px-5 py-4 text-sm sm:grid-cols-2 lg:items-center lg:gap-4", COLUMNS)}>
      <div className="grid min-w-0 gap-0.5">
        <p className="truncate font-medium">{booking.guestName || "Guest"}</p>
        <a
          href={`mailto:${booking.guestEmail}`}
          className="truncate text-muted-foreground underline-offset-4 hover:underline"
        >
          {booking.guestEmail}
        </a>
      </div>

      <div className="grid min-w-0 gap-0.5">
        <Link href={`/apartments/${booking.apartmentId}`} className="truncate font-medium hover:underline">
          {booking.apartmentName}
        </Link>
        <p className="text-muted-foreground">
          {start && end ? formatDateRange(start, end) : "—"}
          {nights > 0 && <span> · {pluralize(nights, "night")}</span>}
        </p>
      </div>

      <p className="flex items-center gap-2 font-semibold tabular-nums lg:justify-end">
        <span className="font-normal text-muted-foreground lg:sr-only">Total</span>
        {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency)}
      </p>

      <div>
        <BookingStatusBadge status={booking.status} />
      </div>

      <p className="text-muted-foreground">
        <span className="lg:sr-only">Created </span>
        {Number.isNaN(created.getTime()) ? (
          "—"
        ) : (
          <time dateTime={booking.createdOnUtc} title={format(created, "PPpp")}>
            {format(created, "MMM d, yyyy")}
          </time>
        )}
      </p>

      <div className="sm:col-span-2 lg:col-span-1">
        <AdminBookingActions booking={booking} />
      </div>
    </li>
  );
}

export function AdminBookingsSkeleton({ rowsOnly = false }: { rowsOnly?: boolean }) {
  return (
    <div className="grid gap-6" role="status" aria-label="Loading bookings">
      {!rowsOnly && (
        <div className="flex gap-2">
          {Array.from({ length: 5 }, (_, i) => (
            <Skeleton key={i} className="h-9 w-28 rounded-full" />
          ))}
        </div>
      )}
      <div className="divide-y rounded-2xl border">
        {Array.from({ length: 6 }, (_, i) => (
          <div key={i} className="grid gap-3 px-5 py-4 sm:grid-cols-2 lg:grid-cols-[1.3fr_1.6fr_7rem_9.5rem_6.5rem_1.2fr] lg:items-center">
            <div className="grid gap-1.5">
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-3.5 w-1/2" />
            </div>
            <div className="grid gap-1.5">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3.5 w-1/2" />
            </div>
            <Skeleton className="h-4 w-16 lg:justify-self-end" />
            <Skeleton className="h-5 w-24 rounded-full" />
            <Skeleton className="h-4 w-20" />
            <Skeleton className="h-8 w-24" />
          </div>
        ))}
      </div>
    </div>
  );
}

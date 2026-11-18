import { SearchX, X } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { Container } from "@/components/shared/container";
import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { ApartmentGrid } from "@/features/apartments/components/apartment-grid";
import { SortSelect } from "@/features/apartments/components/sort-select";
import {
  type ApartmentSearch,
  buildUrl,
  PAGE_SIZE,
  parseApartmentSearch,
  SORT_OPTIONS,
  toSearchParams,
  totalPages,
} from "@/features/apartments/search-params";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { formatDateRange, toDateOnly } from "@/lib/dates";
import { pluralize } from "@/lib/format";

export async function generateMetadata({ searchParams }: PageProps<"/search">): Promise<Metadata> {
  const { q } = parseApartmentSearch(await searchParams);
  return { title: q ? `Stays in ${q}` : "Search stays" };
}

function activeFilters(search: ApartmentSearch) {
  const filters: { key: string; label: string; without: Partial<ApartmentSearch> }[] = [];
  if (search.q) filters.push({ key: "q", label: search.q, without: { q: undefined } });
  if (search.range)
    filters.push({
      key: "dates",
      label: formatDateRange(search.range.from, search.range.to),
      without: { range: undefined },
    });
  if (search.guests)
    filters.push({ key: "guests", label: pluralize(search.guests, "guest"), without: { guests: undefined } });
  if (search.rooms)
    filters.push({ key: "rooms", label: `${pluralize(search.rooms, "room")}+`, without: { rooms: undefined } });
  return filters;
}

export default async function SearchPage({ searchParams }: PageProps<"/search">) {
  const search = parseApartmentSearch(await searchParams);
  const sort = SORT_OPTIONS[search.sort];
  const http = await getServerHttp();

  const result = await attempt(() =>
    apartmentsApi.search(http, {
      page: search.page,
      pageSize: PAGE_SIZE,
      searchTerm: search.q,
      guestCount: search.guests,
      roomCount: search.rooms,
      startDate: search.range ? toDateOnly(search.range.from) : undefined,
      endDate: search.range ? toDateOnly(search.range.to) : undefined,
      sortColumn: "column" in sort ? sort.column : undefined,
      sortOrder: "order" in sort ? sort.order : undefined,
    }),
  );

  const params = toSearchParams(search);
  const filters = activeFilters(search);
  // Carry dates and guests through to the listing so the booking panel is prefilled.
  const cardQuery = new URLSearchParams(
    Object.entries({ startDate: params.startDate, endDate: params.endDate, guests: params.guests }).filter(
      (e): e is [string, string] => Boolean(e[1]),
    ),
  ).toString();

  return (
    <Container className="grid gap-8">
      <header className="grid gap-4">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="grid gap-1">
            <h1 className="text-2xl font-semibold sm:text-3xl">
              {search.q ? `Stays in “${search.q}”` : "Search stays"}
            </h1>
            <p className="text-sm text-muted-foreground" aria-live="polite">
              {result.ok
                ? `${pluralize(result.data.totalCount, "stay")} found`
                : "Results are unavailable right now"}
            </p>
          </div>
          <SortSelect value={search.sort} />
        </div>

        {filters.length > 0 && (
          <ul className="flex flex-wrap items-center gap-2" aria-label="Active filters">
            {filters.map((filter) => (
              <li key={filter.key}>
                <Link
                  href={buildUrl("/search", toSearchParams({ ...search, ...filter.without, page: 1 }))}
                  className="inline-flex h-8 items-center gap-1.5 rounded-full border bg-card pr-2.5 pl-3 text-sm transition-colors hover:bg-muted"
                  aria-label={`Remove filter: ${filter.label}`}
                >
                  {filter.label}
                  <X className="size-3.5 text-muted-foreground" aria-hidden="true" />
                </Link>
              </li>
            ))}
            <li>
              <Link href="/search" className="px-2 text-sm font-medium underline-offset-4 hover:underline">
                Clear all
              </Link>
            </li>
          </ul>
        )}
      </header>

      {!result.ok ? (
        <ErrorState
          title="We can't search stays right now"
          description={result.error.message}
          action={
            <Button asChild variant="outline">
              <Link href={buildUrl("/search", params)} prefetch={false}>
                Try again
              </Link>
            </Button>
          }
        />
      ) : result.data.items.length === 0 ? (
        <EmptyState
          icon={SearchX}
          title="No stays match your search"
          description="Try another destination, fewer guests or rooms, or clear your filters."
          action={
            <Button asChild>
              <Link href="/search">Clear filters</Link>
            </Button>
          }
        />
      ) : (
        <>
          <ApartmentGrid apartments={result.data.items} query={cardQuery} />
          <Pagination
            page={search.page}
            totalPages={totalPages(result.data.totalCount, PAGE_SIZE)}
            pathname="/search"
            searchParams={params}
          />
        </>
      )}
    </Container>
  );
}

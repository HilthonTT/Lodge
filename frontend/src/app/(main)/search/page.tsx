import { SearchX, X } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";

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
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { formatDateRange, toDateOnly } from "@/lib/dates";

export async function generateMetadata({ searchParams }: PageProps<"/search">): Promise<Metadata> {
  const [{ q }, t] = await Promise.all([searchParams.then(parseApartmentSearch), getTranslations("search")]);
  return { title: q ? t("metaTitleQuery", { query: q }) : t("metaTitle") };
}

export default async function SearchPage({ searchParams }: PageProps<"/search">) {
  const search = parseApartmentSearch(await searchParams);
  const sort = SORT_OPTIONS[search.sort];
  const http = await getServerHttp();
  const [result, t, tCommon, tErrors, locale] = await Promise.all([
    attempt(() =>
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
    ),
    getTranslations("search"),
    getTranslations("common"),
    getTranslations("errors.api"),
    getLocale(),
  ]);

  const filters: { key: string; label: string; without: Partial<ApartmentSearch> }[] = [];
  if (search.q) filters.push({ key: "q", label: search.q, without: { q: undefined } });
  if (search.range)
    filters.push({
      key: "dates",
      label: formatDateRange(search.range.from, search.range.to, locale),
      without: { range: undefined },
    });
  if (search.guests)
    filters.push({ key: "guests", label: tCommon("guests", { count: search.guests }), without: { guests: undefined } });
  if (search.rooms)
    filters.push({ key: "rooms", label: t("minRooms", { count: search.rooms }), without: { rooms: undefined } });

  const params = toSearchParams(search);
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
              {search.q ? t("headingQuery", { query: search.q }) : t("heading")}
            </h1>
            <p className="text-sm text-muted-foreground" aria-live="polite">
              {result.ok ? t("found", { count: result.data.totalCount }) : t("unavailable")}
            </p>
          </div>
          <SortSelect value={search.sort} />
        </div>

        {filters.length > 0 && (
          <ul className="flex flex-wrap items-center gap-2" aria-label={t("activeFilters")}>
            {filters.map((filter) => (
              <li key={filter.key}>
                <Link
                  href={buildUrl("/search", toSearchParams({ ...search, ...filter.without, page: 1 }))}
                  className="inline-flex h-8 items-center gap-1.5 rounded-full border bg-card pr-2.5 pl-3 text-sm transition-colors hover:bg-muted"
                  aria-label={t("removeFilter", { label: filter.label })}
                >
                  {filter.label}
                  <X className="size-3.5 text-muted-foreground" aria-hidden="true" />
                </Link>
              </li>
            ))}
            <li>
              <Link href="/search" className="px-2 text-sm font-medium underline-offset-4 hover:underline">
                {t("clearAll")}
              </Link>
            </li>
          </ul>
        )}
      </header>

      {!result.ok ? (
        <ErrorState
          title={t("loadError")}
          description={apiErrorMessage(result.error, tErrors)}
          action={
            <Button asChild variant="outline">
              <Link href={buildUrl("/search", params)} prefetch={false}>
                {tCommon("tryAgain")}
              </Link>
            </Button>
          }
        />
      ) : result.data.items.length === 0 ? (
        <EmptyState
          icon={SearchX}
          title={t("emptyTitle")}
          description={t("emptyDescription")}
          action={
            <Button asChild>
              <Link href="/search">{t("clearFilters")}</Link>
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

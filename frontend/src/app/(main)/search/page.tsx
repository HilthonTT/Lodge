import { SearchX, X } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { SearchResults } from "@/features/apartments/components/search-results";
import { SortSelect } from "@/features/apartments/components/sort-select";
import { formatMonthList } from "@/features/apartments/flexible";
import {
  type ApartmentSearch,
  buildUrl,
  defaultFlexibleMonths,
  PAGE_SIZE,
  parseApartmentSearch,
  toApiParams,
  toSearchParams,
  totalPages,
} from "@/features/apartments/search-params";
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { BASE_CURRENCY, convertAmount } from "@/lib/currency";
import { getServerCurrency, loadExchangeRates } from "@/lib/currency.server";
import { formatDateRange } from "@/lib/dates";
import { formatPriceCompact } from "@/lib/format";

export async function generateMetadata({ searchParams }: PageProps<"/search">): Promise<Metadata> {
  const [{ q }, t] = await Promise.all([searchParams.then(parseApartmentSearch), getTranslations("search")]);
  return { title: q ? t("metaTitleQuery", { query: q }) : t("metaTitle") };
}

export default async function SearchPage({ searchParams }: PageProps<"/search">) {
  const search = parseApartmentSearch(await searchParams);
  const http = await getServerHttp();
  const [result, t, tCommon, tListing, tErrors, locale] = await Promise.all([
    attempt(() => apartmentsApi.search(http, toApiParams(search))),
    getTranslations("search"),
    getTranslations("common"),
    getTranslations("listing"),
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
  if (search.flexible)
    filters.push({
      key: "flexible",
      label: t("flexibleIn", {
        kind: search.flexible,
        months: formatMonthList(search.months.length ? search.months : defaultFlexibleMonths(), locale),
      }),
      without: { flexible: undefined, months: [] },
    });
  if (search.guests)
    filters.push({ key: "guests", label: tCommon("guests", { count: search.guests }), without: { guests: undefined } });
  if (search.minPrice !== undefined || search.maxPrice !== undefined) {
    // Price filters are in USD on the API side; label them in the visitor's currency.
    const [displayCurrency, rates] = await Promise.all([getServerCurrency(), loadExchangeRates()]);
    const label = (usd: number) => {
      const converted = convertAmount(usd, BASE_CURRENCY, displayCurrency, rates?.rates);
      return converted === null
        ? formatPriceCompact(usd, BASE_CURRENCY, locale)
        : formatPriceCompact(converted, displayCurrency, locale);
    };
    const min = search.minPrice !== undefined ? label(search.minPrice) : undefined;
    const max = search.maxPrice !== undefined ? label(search.maxPrice) : undefined;
    filters.push({
      key: "price",
      label: min && max ? `${min} – ${max}` : min ? t("chips.priceFrom", { price: min }) : t("chips.priceUpTo", { price: max ?? "" }),
      without: { minPrice: undefined, maxPrice: undefined },
    });
  }
  for (const type of search.types)
    filters.push({
      key: `type-${type}`,
      label: tListing(`propertyTypes.${type}`),
      without: { types: search.types.filter((x) => x !== type) },
    });
  if (search.bedrooms)
    filters.push({ key: "bedrooms", label: t("chips.bedrooms", { count: search.bedrooms }), without: { bedrooms: undefined } });
  if (search.beds) filters.push({ key: "beds", label: t("chips.beds", { count: search.beds }), without: { beds: undefined } });
  if (search.baths)
    filters.push({ key: "baths", label: t("chips.baths", { count: search.baths }), without: { baths: undefined } });
  for (const amenity of search.amenities)
    filters.push({
      key: `amenity-${amenity}`,
      label: tListing(`amenities.${amenity}`),
      without: { amenities: search.amenities.filter((x) => x !== amenity) },
    });
  if (search.instant) filters.push({ key: "instant", label: t("chips.instantBook"), without: { instant: false } });
  if (search.superhost) filters.push({ key: "superhost", label: t("chips.superhost"), without: { superhost: false } });
  if (search.bounds) filters.push({ key: "bounds", label: t("chips.mapArea"), without: { bounds: undefined } });

  const params = toSearchParams(search);
  // Carry dates and guests through to the listing so the booking panel is prefilled.
  const cardQuery = new URLSearchParams(
    Object.entries({ startDate: params.startDate, endDate: params.endDate, guests: params.guests }).filter(
      (e): e is [string, string] => Boolean(e[1]),
    ),
  ).toString();

  return (
    <Container className="grid max-w-[110rem] gap-8">
      <header className="grid gap-4">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="grid gap-1">
            <h1 className="text-2xl font-semibold sm:text-3xl">
              {search.q ? t("headingQuery", { query: search.q }) : t("heading")}
            </h1>
            <p className="text-sm text-muted-foreground" aria-live="polite" data-testid="search-count">
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
                  scroll={false}
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
      ) : (
        <SearchResults
          apartments={result.data.items}
          query={cardQuery}
          empty={
            <EmptyState
              icon={SearchX}
              title={t("emptyTitle")}
              description={search.bounds ? t("emptyMapDescription") : t("emptyDescription")}
              action={
                <Button asChild>
                  <Link href="/search">{t("clearFilters")}</Link>
                </Button>
              }
            />
          }
          footer={
            <Pagination
              page={search.page}
              totalPages={totalPages(result.data.totalCount, PAGE_SIZE)}
              pathname="/search"
              searchParams={params}
            />
          }
        />
      )}
    </Container>
  );
}

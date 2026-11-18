"use client";

import { Compass, List, Map as MapIcon, SlidersHorizontal, X } from "lucide-react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { useDisplayMoney } from "@/features/payments/currency-provider";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { BASE_CURRENCY } from "@/lib/currency";
import { formatMediumDate, parseDateOnly } from "@/lib/dates";
import { formatPriceCompact } from "@/lib/format";
import { cn } from "@/lib/utils";
import { EXPERIENCE_CATEGORIES } from "@/types/api";

import { CATEGORY_THEME } from "../experience-options";
import { useExperienceSearch } from "../hooks/use-experiences";
import {
  EXPERIENCES_PAGE_SIZE,
  type ExperienceSearch,
  experiencesHref,
  parseExperienceSearch,
  toExperienceApiParams,
} from "../search-params";
import { ExperienceCard, ExperienceCardSkeleton } from "./experience-card";
import type { ExperiencesLeafletMapProps } from "./experiences-leaflet-map";

const ExperiencesMap = dynamic<ExperiencesLeafletMapProps>(() => import("./experiences-leaflet-map"), {
  ssr: false,
  loading: () => <Skeleton className="size-full rounded-none" />,
});

export function ExperiencesExplore() {
  const t = useTranslations("experiences");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const searchParams = useSearchParams();
  const search = useMemo(() => parseExperienceSearch(Object.fromEntries(searchParams.entries())), [searchParams]);
  const results = useExperienceSearch(toExperienceApiParams(search));
  const money = useDisplayMoney();
  const [hovered, setHovered] = useState<string | null>(null);
  const totalPages = results.data ? Math.ceil(results.data.totalCount / EXPERIENCES_PAGE_SIZE) : 0;
  const isMap = search.view === "map";

  const href = (patch: Partial<ExperienceSearch>) => experiencesHref({ ...search, page: 1, ...patch });
  const usd = (value: number) => formatPriceCompact(money.convert(value, BASE_CURRENCY).amount, money.currency, locale);

  const active: { key: string; label: string; without: Partial<ExperienceSearch> }[] = [];
  if (search.q) active.push({ key: "q", label: search.q, without: { q: undefined } });
  if (search.date) {
    const date = parseDateOnly(search.date);
    active.push({ key: "date", label: date ? formatMediumDate(date, locale) : search.date, without: { date: undefined } });
  }
  if (search.guests) active.push({ key: "guests", label: tCommon("guests", { count: search.guests }), without: { guests: undefined } });
  if (search.minPrice !== undefined || search.maxPrice !== undefined)
    active.push({
      key: "price",
      label: t("filters.priceRange", {
        min: search.minPrice !== undefined ? usd(search.minPrice) : "–",
        max: search.maxPrice !== undefined ? usd(search.maxPrice) : "–",
      }),
      without: { minPrice: undefined, maxPrice: undefined },
    });

  const searchParamsRecord: Record<string, string | undefined> = Object.fromEntries(searchParams.entries());

  return (
    <div className="grid gap-6">
      <div className="grid gap-1">
        <h1 className="text-2xl font-semibold sm:text-3xl">
          {search.q ? t("titleQuery", { query: search.q }) : t("title")}
        </h1>
        <p className="text-muted-foreground">
          {results.data ? t("resultCount", { count: results.data.totalCount }) : t("subtitle")}
        </p>
      </div>

      <div className="flex items-center gap-3">
        <nav
          aria-label={t("categoriesLabel")}
          className="-mx-1 flex min-w-0 flex-1 gap-2 overflow-x-auto px-1 pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
        >
          <Link
            href={href({ category: undefined })}
            aria-current={!search.category ? "page" : undefined}
            scroll={false}
            className={chip(!search.category)}
          >
            <Compass className="size-4" aria-hidden="true" />
            {t("allCategories")}
          </Link>
          {EXPERIENCE_CATEGORIES.map((category) => {
            const Icon = CATEGORY_THEME[category].icon;
            const selected = search.category === category;
            return (
              <Link
                key={category}
                href={href({ category: selected ? undefined : category })}
                aria-current={selected ? "page" : undefined}
                scroll={false}
                className={chip(selected)}
                data-testid={`category-${category}`}
              >
                <Icon className="size-4" aria-hidden="true" />
                {t(`categories.${category}`)}
              </Link>
            );
          })}
        </nav>
        <PriceFilter search={search} />
      </div>

      {active.length > 0 && (
        <ul className="flex flex-wrap items-center gap-2" aria-label={t("filters.active")}>
          {active.map((f) => (
            <li key={f.key}>
              <Link
                href={href(f.without)}
                scroll={false}
                className="inline-flex h-8 items-center gap-1.5 rounded-full border bg-card px-3 text-sm hover:bg-muted"
                aria-label={t("filters.remove", { filter: f.label })}
              >
                {f.label} <X className="size-3.5" aria-hidden="true" />
              </Link>
            </li>
          ))}
          <li>
            <Link href={experiencesHref({ category: search.category, view: search.view })} className="px-2 text-sm font-medium underline-offset-4 hover:underline">
              {t("filters.clear")}
            </Link>
          </li>
        </ul>
      )}

      {results.isPending ? (
        <div className="grid gap-x-6 gap-y-10 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4" role="status" aria-label={t("loading")}>
          {Array.from({ length: 8 }, (_, i) => (
            <ExperienceCardSkeleton key={i} />
          ))}
        </div>
      ) : results.isError ? (
        <ErrorState
          title={t("loadError")}
          description={errorMessage(results.error)}
          action={
            <Button variant="outline" onClick={() => results.refetch()}>
              {tCommon("tryAgain")}
            </Button>
          }
        />
      ) : results.data.items.length === 0 ? (
        <EmptyState
          icon={Compass}
          title={t("emptyTitle")}
          description={t("emptyDescription")}
          action={
            <Button asChild variant="outline">
              <Link href="/experiences">{t("filters.clear")}</Link>
            </Button>
          }
        />
      ) : (
        <div className={cn(isMap && "grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] xl:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]")}>
          <div className={cn("grid content-start gap-8", results.isPlaceholderData && "opacity-60 transition-opacity")}>
            <ul
              className={cn(
                "grid gap-x-6 gap-y-10 sm:grid-cols-2",
                isMap ? "xl:grid-cols-2" : "lg:grid-cols-3 xl:grid-cols-4",
              )}
            >
              {results.data.items.map((experience, index) => (
                <li key={experience.id}>
                  <ExperienceCard
                    experience={experience}
                    priority={index < 4}
                    highlighted={isMap && hovered === experience.id}
                    onHoverChange={isMap ? (on) => setHovered(on ? experience.id : null) : undefined}
                  />
                </li>
              ))}
            </ul>
            <Pagination page={search.page} totalPages={totalPages} pathname="/experiences" searchParams={searchParamsRecord} />
          </div>
          {isMap && (
            <div className="relative isolate order-first h-80 overflow-hidden rounded-2xl border bg-muted lg:sticky lg:top-28 lg:order-none lg:h-[calc(100dvh-9rem)]">
              <ExperiencesMap experiences={results.data.items} hoveredId={hovered} onHover={setHovered} />
            </div>
          )}
        </div>
      )}

      <div className="pointer-events-none sticky bottom-6 z-30 flex justify-center">
        <Button asChild className="pointer-events-auto rounded-full shadow-float" size="lg">
          <Link href={href({ view: isMap ? "list" : "map", page: search.page })} scroll={false} data-testid="map-toggle">
            {isMap ? <List aria-hidden="true" /> : <MapIcon aria-hidden="true" />}
            {isMap ? t("showList") : t("showMap")}
          </Link>
        </Button>
      </div>
    </div>
  );
}

function chip(active: boolean) {
  return cn(
    "inline-flex h-10 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
    active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
  );
}

/** Min/max price per guest in the display currency; written to the URL in USD (the API compares in USD). */
function PriceFilter({ search }: { search: ExperienceSearch }) {
  const t = useTranslations("experiences.filters");
  const router = useRouter();
  const money = useDisplayMoney();
  const [open, setOpen] = useState(false);
  const toDisplay = (usd: number | undefined) =>
    usd === undefined ? "" : String(Math.round(money.convert(usd, BASE_CURRENCY).amount));
  const [min, setMin] = useState("");
  const [max, setMax] = useState("");
  const activeCount = search.minPrice !== undefined || search.maxPrice !== undefined;

  const toUsd = (value: string) => {
    const n = Number(value);
    if (value === "" || !Number.isFinite(n) || n < 0) return undefined;
    const back = money.convertBack(n, BASE_CURRENCY);
    return Math.round(back ?? n);
  };

  const apply = (event: React.FormEvent) => {
    event.preventDefault();
    setOpen(false);
    router.push(experiencesHref({ ...search, minPrice: toUsd(min), maxPrice: toUsd(max), page: 1 }), { scroll: false });
  };

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) {
          setMin(toDisplay(search.minPrice));
          setMax(toDisplay(search.maxPrice));
        }
        setOpen(next);
      }}
    >
      <PopoverTrigger asChild>
        <Button variant="outline" className={cn("h-10 shrink-0 rounded-full", activeCount && "border-foreground")}>
          <SlidersHorizontal aria-hidden="true" />
          <span className="hidden sm:inline">{t("price")}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80">
        <form onSubmit={apply} className="grid gap-4">
          <div className="grid gap-1">
            <h2 className="font-semibold">{t("priceTitle")}</h2>
            <p className="text-xs text-muted-foreground">{t("priceHint", { currency: money.currency })}</p>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <label className="grid gap-1.5 text-sm">
              <span className="font-medium">{t("min")}</span>
              <Input type="number" inputMode="numeric" min={0} value={min} onChange={(e) => setMin(e.target.value)} />
            </label>
            <label className="grid gap-1.5 text-sm">
              <span className="font-medium">{t("max")}</span>
              <Input type="number" inputMode="numeric" min={0} value={max} onChange={(e) => setMax(e.target.value)} />
            </label>
          </div>
          <div className="flex justify-between gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                setMin("");
                setMax("");
              }}
            >
              {t("reset")}
            </Button>
            <Button type="submit">{t("apply")}</Button>
          </div>
        </form>
      </PopoverContent>
    </Popover>
  );
}

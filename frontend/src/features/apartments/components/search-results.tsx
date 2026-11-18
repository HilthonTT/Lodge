"use client";

import { List, Map as MapIcon, MapPin } from "lucide-react";
import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useCallback, useMemo, useState, useTransition } from "react";

import { Checkbox } from "@/components/ui/checkbox";
import { Skeleton } from "@/components/ui/skeleton";
import { useMediaQuery } from "@/hooks/use-media-query";
import { cn } from "@/lib/utils";
import type { Apartment } from "@/types/api";

import {
  buildUrl,
  type MapBounds,
  parseApartmentSearch,
  toSearchParams,
} from "../search-params";
import { ApartmentCard } from "./apartment-card";

const SearchLeafletMap = dynamic(() => import("./search-leaflet-map"), {
  ssr: false,
  loading: () => <Skeleton className="size-full rounded-none" />,
});

type SearchResultsProps = {
  apartments: Apartment[];
  /** Query string forwarded to listing links (dates and guests). */
  query?: string;
  /** Shown in the list column when there are no results (the map stays usable). */
  empty?: React.ReactNode;
  /** Pagination, below the list. */
  footer?: React.ReactNode;
};

/**
 * Results as a list next to a map (desktop) or with a list/map toggle (mobile).
 * Hovering a card highlights its price marker and vice versa.
 */
export function SearchResults({
  apartments,
  query,
  empty,
  footer,
}: SearchResultsProps) {
  const t = useTranslations("search.map");
  const router = useRouter();
  const searchParams = useSearchParams();
  const isDesktop = useMediaQuery("(min-width: 1024px)");
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const [showMap, setShowMap] = useState(false);
  const [isPending, startTransition] = useTransition();

  const search = useMemo(
    () => parseApartmentSearch(Object.fromEntries(searchParams.entries())),
    [searchParams],
  );
  const moveSearch = Boolean(search.bounds);
  // While waiting for the first bounds after ticking the box, keep the checkbox ticked.
  const [moveSearchPending, setMoveSearchPending] = useState(false);

  const navigate = useCallback(
    (bounds: MapBounds | undefined) => {
      const current = parseApartmentSearch(
        Object.fromEntries(
          new URLSearchParams(window.location.search).entries(),
        ),
      );
      startTransition(() =>
        router.replace(
          buildUrl("/search", toSearchParams({ ...current, bounds, page: 1 })),
          { scroll: false },
        ),
      );
    },
    [router],
  );

  const onBoundsChange = useCallback(
    (bounds: MapBounds) => navigate(bounds),
    [navigate],
  );
  const onToggleMoveSearch = (checked: boolean) => {
    // Turning it on: the map reports its current bounds right away (see MoveListener).
    if (!checked) navigate(undefined);
    else setMoveSearchPending(true);
  };
  const moveSearchOn = moveSearch || moveSearchPending;
  if (moveSearchPending && moveSearch) setMoveSearchPending(false);

  const mapVisible = isDesktop || showMap;

  return (
    <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.9fr)] xl:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]">
      <div
        className={cn(
          "grid content-start gap-8",
          showMap && "hidden lg:grid",
          isPending && "opacity-60 transition-opacity",
        )}
      >
        {apartments.length === 0 ? (
          empty
        ) : (
          <ul className="grid grid-cols-1 gap-x-6 gap-y-10 sm:grid-cols-2">
            {apartments.map((apartment, index) => (
              <li key={apartment.id}>
                <ApartmentCard
                  apartment={apartment}
                  query={query}
                  priority={index < 2}
                  highlighted={hoveredId === apartment.id}
                  onHoverChange={(hovered) =>
                    setHoveredId((current) =>
                      hovered
                        ? apartment.id
                        : current === apartment.id
                          ? null
                          : current,
                    )
                  }
                  sizes="(min-width: 1280px) 22vw, (min-width: 1024px) 26vw, (min-width: 640px) 50vw, 100vw"
                />
              </li>
            ))}
          </ul>
        )}
        {footer}
      </div>

      <div
        className={cn(
          "relative isolate overflow-hidden rounded-2xl border bg-muted",
          "h-[calc(100dvh-12rem)] lg:sticky lg:top-24 lg:h-[calc(100dvh-8rem)]",
          !showMap && "hidden lg:block",
        )}
        data-testid="search-map"
      >
        {mapVisible ? (
          <SearchLeafletMap
            apartments={apartments}
            query={query}
            hoveredId={hoveredId}
            onHover={setHoveredId}
            moveSearch={moveSearchOn}
            bounds={search.bounds}
            onBoundsChange={onBoundsChange}
          />
        ) : (
          <div className="grid size-full place-items-center text-muted-foreground">
            <MapPin className="size-6" aria-hidden="true" />
          </div>
        )}
        <label className="absolute top-3 left-1/2 z-[1000] flex -translate-x-1/2 cursor-pointer items-center gap-2 rounded-full border bg-background px-4 py-2 text-sm font-medium whitespace-nowrap shadow-float">
          <Checkbox
            checked={moveSearchOn}
            onCheckedChange={(c) => onToggleMoveSearch(c === true)}
          />
          {t("searchAsIMove")}
        </label>
      </div>

      <button
        type="button"
        onClick={() => setShowMap((v) => !v)}
        className="fixed bottom-6 left-1/2 z-30 inline-flex -translate-x-1/2 items-center gap-2 rounded-full bg-foreground px-5 py-3 text-sm font-semibold text-background shadow-float transition-transform hover:scale-105 active:scale-95 lg:hidden"
      >
        {showMap ? (
          <>
            {t("showList")} <List className="size-4" aria-hidden="true" />
          </>
        ) : (
          <>
            {t("showMap")} <MapIcon className="size-4" aria-hidden="true" />
          </>
        )}
      </button>
    </div>
  );
}

"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { Amenity } from "@/types/api";

import { AMENITY_ICONS, CATEGORY_TAGS, isCategoryTag } from "../listing-options";
import { buildUrl, EMPTY_SEARCH, parseApartmentSearch, toSearchParams } from "../search-params";
import { FiltersDialog } from "./filters-dialog";

const PATHS = ["/", "/search"];

/**
 * Airbnb-style category tabs under the navbar (on / and /search). A category is a
 * tag in `amenities`; picking one replaces any other category but keeps the
 * real amenities chosen in Filters.
 */
export function CategoryBar() {
  const pathname = usePathname();
  if (!PATHS.includes(pathname)) return null;
  return <CategoryBarInner pathname={pathname} />;
}

function CategoryBarInner({ pathname }: { pathname: string }) {
  const t = useTranslations("search.categories");
  const tListing = useTranslations("listing");
  const searchParams = useSearchParams();
  const scroller = useRef<HTMLDivElement>(null);
  const [edges, setEdges] = useState({ start: true, end: true });

  const search = useMemo(
    () => (pathname === "/search" ? parseApartmentSearch(Object.fromEntries(searchParams.entries())) : EMPTY_SEARCH),
    [pathname, searchParams],
  );
  const active = search.amenities.find(isCategoryTag);

  const hrefFor = (tag: Amenity) => {
    const others = search.amenities.filter((a) => !isCategoryTag(a));
    const amenities = active === tag ? others : [...others, tag];
    return buildUrl("/search", toSearchParams({ ...search, amenities, page: 1 }));
  };

  const updateEdges = useCallback(() => {
    const node = scroller.current;
    if (!node) return;
    setEdges({
      start: node.scrollLeft <= 4,
      end: node.scrollLeft + node.clientWidth >= node.scrollWidth - 4,
    });
  }, []);

  useEffect(() => {
    updateEdges();
    const node = scroller.current;
    if (!node) return;
    const observer = new ResizeObserver(updateEdges);
    observer.observe(node);
    return () => observer.disconnect();
  }, [updateEdges]);

  const scrollBy = (direction: 1 | -1) =>
    scroller.current?.scrollBy({ left: direction * scroller.current.clientWidth * 0.7, behavior: "smooth" });

  return (
    <div className="border-b bg-background">
      <Container className="flex items-center gap-4">
        <div className="relative min-w-0 flex-1">
          {!edges.start && (
            <Button
              variant="outline"
              size="icon-sm"
              className="absolute top-1/2 left-0 z-10 hidden -translate-y-1/2 rounded-full shadow-card sm:inline-flex"
              onClick={() => scrollBy(-1)}
              aria-label={t("scrollBack")}
            >
              <ChevronLeft />
            </Button>
          )}
          <div
            ref={scroller}
            onScroll={updateEdges}
            className={cn(
              "flex gap-7 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden",
              !edges.start && "sm:[mask-image:linear-gradient(to_right,transparent,black_3rem)]",
              !edges.end && "sm:[mask-image:linear-gradient(to_left,transparent,black_3rem)]",
              !edges.start &&
                !edges.end &&
                "sm:[mask-image:linear-gradient(to_right,transparent,black_3rem,black_calc(100%-3rem),transparent)]",
            )}
          >
            <nav aria-label={t("label")} className="contents">
              {CATEGORY_TAGS.map((tag) => {
                const Icon = AMENITY_ICONS[tag];
                const selected = active === tag;
                return (
                  <Link
                    key={tag}
                    href={hrefFor(tag)}
                    aria-current={selected ? "page" : undefined}
                    scroll={false}
                    className={cn(
                      "group flex shrink-0 flex-col items-center gap-1.5 border-b-2 pt-3 pb-2.5 text-xs font-medium transition-colors outline-none focus-visible:text-foreground",
                      selected
                        ? "border-foreground text-foreground"
                        : "border-transparent text-muted-foreground hover:border-border hover:text-foreground",
                    )}
                  >
                    <Icon className="size-6" strokeWidth={selected ? 2.2 : 1.6} aria-hidden="true" />
                    <span className="whitespace-nowrap">{tListing(`amenities.${tag}`)}</span>
                  </Link>
                );
              })}
            </nav>
          </div>
          {!edges.end && (
            <Button
              variant="outline"
              size="icon-sm"
              className="absolute top-1/2 right-0 z-10 hidden -translate-y-1/2 rounded-full shadow-card sm:inline-flex"
              onClick={() => scrollBy(1)}
              aria-label={t("scrollForward")}
            >
              <ChevronRight />
            </Button>
          )}
        </div>
        <FiltersDialog search={search} className="shrink-0" />
      </Container>
    </div>
  );
}

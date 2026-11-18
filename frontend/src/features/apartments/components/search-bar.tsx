"use client";

import { Search } from "lucide-react";
import { usePathname, useSearchParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { formatShortDate } from "@/lib/dates";
import { cn } from "@/lib/utils";

import { formatMonthList } from "../flexible";
import { defaultFlexibleMonths, parseApartmentSearch } from "../search-params";
import { SearchDialog, type SearchSection } from "./search-dialog";

/** Navbar search pill. Opens the search dialog focused on the clicked segment. */
export function SearchBar({ className }: { className?: string }) {
  const t = useTranslations();
  const locale = useLocale();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [open, setOpen] = useState(false);
  const [focus, setFocus] = useState<SearchSection>("where");

  // Only reflect the URL when we're actually on the search page.
  const current = useMemo(
    () =>
      parseApartmentSearch(
        pathname === "/search"
          ? Object.fromEntries(searchParams.entries())
          : {},
      ),
    [pathname, searchParams],
  );

  const openAt = (section: SearchSection) => {
    setFocus(section);
    setOpen(true);
  };

  const whereLabel = current.q || t("search.bar.anywhere");
  const whenLabel = current.range
    ? `${formatShortDate(current.range.from, locale)} – ${formatShortDate(current.range.to, locale)}`
    : current.flexible
      ? t("search.flexibleIn", {
          kind: current.flexible,
          months: formatMonthList(
            current.months.length ? current.months : defaultFlexibleMonths(),
            locale,
          ),
        })
      : t("search.bar.anyWeek");
  const whoLabel = current.guests
    ? t("common.guests", { count: current.guests })
    : t("search.bar.addGuests");

  const segment =
    "h-full rounded-full px-4 text-sm transition-colors hover:bg-muted focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none";

  return (
    <>
      {/* Desktop: three segments */}
      <div
        className={cn(
          "hidden h-12 items-center rounded-full border bg-card p-1 shadow-card transition-shadow hover:shadow-float md:flex",
          className,
        )}
      >
        <button
          type="button"
          className={cn(segment, "max-w-48 truncate font-medium")}
          onClick={() => openAt("where")}
        >
          {whereLabel}
        </button>
        <span className="h-6 w-px bg-border" aria-hidden="true" />
        <button
          type="button"
          className={cn(segment, "max-w-56 truncate font-medium")}
          onClick={() => openAt("when")}
        >
          {whenLabel}
        </button>
        <span className="h-6 w-px bg-border" aria-hidden="true" />
        <button
          type="button"
          className={cn(
            segment,
            current.guests ? "font-medium" : "text-muted-foreground",
          )}
          onClick={() => openAt("who")}
        >
          {whoLabel}
        </button>
        <button
          type="button"
          onClick={() => openAt("where")}
          className="ml-1 grid size-10 place-items-center rounded-full bg-primary text-primary-foreground transition hover:bg-primary/90 focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
          aria-label={t("search.bar.openSearch")}
        >
          <Search className="size-4" aria-hidden="true" />
        </button>
      </div>

      {/* Mobile: single pill */}
      <button
        type="button"
        onClick={() => openAt("where")}
        className={cn(
          "flex h-12 w-full min-w-0 items-center gap-3 rounded-full border bg-card px-4 text-left shadow-card md:hidden",
          className,
        )}
      >
        <Search className="size-4 shrink-0" aria-hidden="true" />
        <span className="grid min-w-0 leading-tight">
          <span className="truncate text-sm font-medium">
            {current.q || t("search.bar.whereTo")}
          </span>
          <span className="truncate text-xs text-muted-foreground">
            {whenLabel} · {whoLabel}
          </span>
        </span>
      </button>

      <SearchDialog
        open={open}
        onOpenChange={setOpen}
        initial={current}
        focus={focus}
      />
    </>
  );
}

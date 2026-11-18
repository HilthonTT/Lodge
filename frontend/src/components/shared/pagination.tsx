import { ChevronLeft, ChevronRight } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type PaginationProps = {
  page: number;
  totalPages: number;
  /** Path to paginate, e.g. "/search". */
  pathname: string;
  /** Current search params; `page` is replaced. */
  searchParams?: Record<string, string | undefined>;
};

/** Returns page numbers to show, with `null` for gaps: 1 … 4 5 6 … 12 */
export function getPageWindow(page: number, totalPages: number): (number | null)[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);
  const pages = new Set([1, totalPages, page - 1, page, page + 1]);
  if (page <= 3) [2, 3, 4].forEach((p) => pages.add(p));
  if (page >= totalPages - 2) [totalPages - 3, totalPages - 2, totalPages - 1].forEach((p) => pages.add(p));
  const sorted = [...pages].filter((p) => p >= 1 && p <= totalPages).sort((a, b) => a - b);
  const result: (number | null)[] = [];
  sorted.forEach((p, i) => {
    const prev = sorted[i - 1];
    if (prev !== undefined && p - prev > 1) result.push(null);
    result.push(p);
  });
  return result;
}

/** Link-based pagination: works without JS, preserves filters, scrolls to top. */
export function Pagination({ page, totalPages, pathname, searchParams = {} }: PaginationProps) {
  const t = useTranslations("pagination");

  if (totalPages <= 1) return null;

  const href = (target: number) => {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(searchParams)) {
      if (value && key !== "page") params.set(key, value);
    }
    if (target > 1) params.set("page", String(target));
    const query = params.toString();
    return query ? `${pathname}?${query}` : pathname;
  };

  const item = "min-w-10 rounded-full";

  return (
    <nav aria-label={t("label")} className="flex items-center justify-center gap-1">
      {page > 1 ? (
        <Link
          href={href(page - 1)}
          className={cn(buttonVariants({ variant: "ghost" }), "rounded-full")}
          rel="prev"
        >
          <ChevronLeft aria-hidden="true" />
          <span className="hidden sm:inline">{t("previous")}</span>
          <span className="sr-only sm:hidden">{t("previousPage")}</span>
        </Link>
      ) : (
        <span className={cn(buttonVariants({ variant: "ghost" }), "pointer-events-none rounded-full opacity-40")} aria-hidden="true">
          <ChevronLeft />
          <span className="hidden sm:inline">{t("previous")}</span>
        </span>
      )}

      <ul className="hidden items-center gap-1 sm:flex">
        {getPageWindow(page, totalPages).map((p, i) =>
          p === null ? (
            <li key={`gap-${i}`} aria-hidden="true" className="px-1 text-muted-foreground">
              …
            </li>
          ) : (
            <li key={p}>
              <Link
                href={href(p)}
                aria-label={t("page", { page: String(p) })}
                aria-current={p === page ? "page" : undefined}
                className={cn(
                  buttonVariants({ variant: p === page ? "default" : "ghost", size: "icon" }),
                  item,
                )}
              >
                {p}
              </Link>
            </li>
          ),
        )}
      </ul>
      <p className="px-3 text-sm text-muted-foreground sm:hidden">
        {t("pageOf", { page: String(page), total: String(totalPages) })}
      </p>

      {page < totalPages ? (
        <Link
          href={href(page + 1)}
          className={cn(buttonVariants({ variant: "ghost" }), "rounded-full")}
          rel="next"
        >
          <span className="hidden sm:inline">{t("next")}</span>
          <span className="sr-only sm:hidden">{t("nextPage")}</span>
          <ChevronRight aria-hidden="true" />
        </Link>
      ) : (
        <span className={cn(buttonVariants({ variant: "ghost" }), "pointer-events-none rounded-full opacity-40")} aria-hidden="true">
          <span className="hidden sm:inline">{t("next")}</span>
          <ChevronRight />
        </span>
      )}
    </nav>
  );
}

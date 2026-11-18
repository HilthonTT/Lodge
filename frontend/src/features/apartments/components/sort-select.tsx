"use client";

import { ArrowUpDown } from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useTransition } from "react";

import { cn } from "@/lib/utils";

import { SORT_OPTIONS, type SortKey } from "../search-params";

export function SortSelect({ value }: { value: SortKey }) {
  const t = useTranslations("search.sort");
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const onChange = (next: string) => {
    const params = new URLSearchParams(searchParams.toString());
    if (next === "recommended") params.delete("sort");
    else params.set("sort", next);
    params.delete("page");
    const query = params.toString();
    startTransition(() => router.push(query ? `${pathname}?${query}` : pathname));
  };

  return (
    <div className="relative">
      <label htmlFor="sort" className="sr-only">
        {t("label")}
      </label>
      <ArrowUpDown
        className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden="true"
      />
      <select
        id="sort"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={cn(
          "h-10 appearance-none rounded-full border border-input bg-background pr-4 pl-10 text-sm font-medium shadow-xs outline-none focus-visible:ring-[3px] focus-visible:ring-ring/30 dark:bg-input/20",
          isPending && "opacity-60",
        )}
      >
        {(Object.keys(SORT_OPTIONS) as SortKey[]).map((key) => (
          <option key={key} value={key}>
            {t(SORT_OPTIONS[key].labelKey)}
          </option>
        ))}
      </select>
    </div>
  );
}

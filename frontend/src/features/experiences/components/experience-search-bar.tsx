"use client";

import { Search } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { toDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";

import { experiencesHref, parseExperienceSearch } from "../search-params";

/** Navbar search on /experiences: where (city or keyword), date and guests, inline. */
export function ExperienceSearchBar({ className }: { className?: string }) {
  const t = useTranslations("experiences.search");
  const router = useRouter();
  const searchParams = useSearchParams();
  const current = useMemo(() => parseExperienceSearch(Object.fromEntries(searchParams.entries())), [searchParams]);
  // Re-seed the inputs when the URL changes (e.g. a category chip or "Clear").
  const [seed, setSeed] = useState(searchParams.toString());
  const [q, setQ] = useState(current.q ?? "");
  const [date, setDate] = useState(current.date ?? "");
  const [guests, setGuests] = useState(current.guests ? String(current.guests) : "");
  if (seed !== searchParams.toString()) {
    setSeed(searchParams.toString());
    setQ(current.q ?? "");
    setDate(current.date ?? "");
    setGuests(current.guests ? String(current.guests) : "");
  }

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    router.push(
      experiencesHref({
        ...current,
        q: q.trim() || undefined,
        date: date || undefined,
        guests: Number(guests) > 0 ? Number(guests) : undefined,
        page: 1,
      }),
    );
  };

  const segment = "grid min-w-0 gap-0 rounded-full px-4 py-1 text-left transition-colors hover:bg-muted focus-within:bg-muted";
  const label = "text-[0.7rem] font-semibold";
  const input = "w-full min-w-0 bg-transparent text-sm outline-none placeholder:text-muted-foreground";

  return (
    <form
      role="search"
      aria-label={t("label")}
      onSubmit={submit}
      className={cn("flex h-12 w-full max-w-xl items-center rounded-full border bg-card p-1 shadow-card transition-shadow hover:shadow-float", className)}
    >
      <label className={cn(segment, "flex-[1.4]")}>
        <span className={label}>{t("where")}</span>
        <input
          type="search"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder={t("wherePlaceholder")}
          maxLength={100}
          className={input}
          data-testid="experience-search-q"
        />
      </label>
      <span className="hidden h-6 w-px bg-border md:block" aria-hidden="true" />
      <label className={cn(segment, "hidden flex-1 md:grid")}>
        <span className={label}>{t("date")}</span>
        <input
          type="date"
          value={date}
          min={toDateOnly(new Date())}
          onChange={(e) => setDate(e.target.value)}
          className={cn(input, "[color-scheme:light] dark:[color-scheme:dark]")}
        />
      </label>
      <span className="hidden h-6 w-px bg-border md:block" aria-hidden="true" />
      <label className={cn(segment, "hidden w-24 md:grid")}>
        <span className={label}>{t("guests")}</span>
        <input
          type="number"
          inputMode="numeric"
          min={1}
          max={50}
          value={guests}
          onChange={(e) => setGuests(e.target.value)}
          placeholder={t("guestsPlaceholder")}
          className={input}
        />
      </label>
      <button
        type="submit"
        className="ml-1 grid size-10 shrink-0 place-items-center rounded-full bg-primary text-primary-foreground transition hover:bg-primary/90 focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
        aria-label={t("submit")}
      >
        <Search className="size-4" aria-hidden="true" />
      </button>
    </form>
  );
}

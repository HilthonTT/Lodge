"use client";

import { Loader2, MapPin, Search } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import type { LocationSuggestion } from "@/types/api";

import { useLocationSuggestions } from "../hooks/use-search-helpers";

type LocationAutocompleteProps = {
  id: string;
  value: string;
  onChange: (value: string) => void;
  /** Called when a suggestion is picked (click or Enter). */
  onSelect?: (suggestion: LocationSuggestion) => void;
  placeholder?: string;
  autoFocus?: boolean;
};

/** Destination input with an ARIA combobox listing GET /locations/suggest results. */
export function LocationAutocomplete({ id, value, onChange, onSelect, placeholder, autoFocus }: LocationAutocompleteProps) {
  const t = useTranslations("search.dialog");
  const listId = useId();
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const suggestions = useLocationSuggestions(value);
  const items = suggestions.enabled ? (suggestions.data ?? []) : [];
  const expanded = open && suggestions.enabled && (items.length > 0 || (suggestions.settled && !suggestions.isFetching));

  const pick = (suggestion: LocationSuggestion) => {
    onChange(suggestion.city || suggestion.country);
    onSelect?.(suggestion);
    setOpen(false);
    setActive(-1);
  };

  const onKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setOpen(true);
      if (items.length) setActive((i) => (i + 1) % items.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setOpen(true);
      if (items.length) setActive((i) => (i <= 0 ? items.length - 1 : i - 1));
    } else if (event.key === "Enter" && expanded && active >= 0 && items[active]) {
      event.preventDefault();
      pick(items[active]);
    } else if (event.key === "Escape" && expanded) {
      // Close the list without closing the dialog.
      event.preventDefault();
      event.stopPropagation();
      setOpen(false);
      setActive(-1);
    }
  };

  return (
    <div className="relative">
      <Search
        className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden="true"
      />
      <Input
        id={id}
        role="combobox"
        aria-expanded={expanded}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={expanded && active >= 0 ? `${listId}-${active}` : undefined}
        value={value}
        onChange={(e) => {
          onChange(e.target.value);
          setOpen(true);
          setActive(-1);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={onKeyDown}
        placeholder={placeholder}
        autoComplete="off"
        autoFocus={autoFocus}
        maxLength={100}
        className="pr-10 pl-10"
      />
      {suggestions.isFetching && (
        <Loader2
          className="absolute top-1/2 right-3.5 size-4 -translate-y-1/2 animate-spin text-muted-foreground"
          aria-hidden="true"
        />
      )}
      <ul
        id={listId}
        role="listbox"
        aria-label={t("suggestions")}
        hidden={!expanded}
        className="absolute inset-x-0 top-full z-20 mt-2 grid max-h-80 overflow-y-auto rounded-2xl border bg-popover p-2 shadow-float"
      >
        {items.length === 0 ? (
          <li className="px-3 py-2.5 text-sm text-muted-foreground" role="presentation">
            {t("noSuggestions")}
          </li>
        ) : (
          items.map((item, index) => (
            <li
              key={`${item.label}-${index}`}
              id={`${listId}-${index}`}
              role="option"
              aria-selected={index === active}
              // Keep focus in the input so blur doesn't close the list before the click lands.
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => pick(item)}
              onMouseEnter={() => setActive(index)}
              className={cn(
                "flex cursor-pointer items-center gap-3 rounded-xl px-3 py-2.5 text-sm",
                index === active && "bg-muted",
              )}
            >
              <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-muted text-muted-foreground">
                <MapPin className="size-5" aria-hidden="true" />
              </span>
              <span className="grid min-w-0">
                <span className="truncate font-medium">{item.label}</span>
                <span className="text-xs text-muted-foreground">{t("staysCount", { count: item.listingCount })}</span>
              </span>
            </li>
          ))
        )}
      </ul>
    </div>
  );
}

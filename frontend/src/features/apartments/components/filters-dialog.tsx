"use client";

import {
  Award,
  Loader2,
  Minus,
  Plus,
  SlidersHorizontal,
  Zap,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState, useTransition } from "react";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import { useDisplayMoney } from "@/features/payments/currency-provider";
import { BASE_CURRENCY } from "@/lib/currency";
import { formatPriceCompact } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type Amenity, PROPERTY_TYPES, type PropertyType } from "@/types/api";

import { usePriceRange, useSearchCount } from "../hooks/use-search-helpers";
import {
  AMENITY_GROUPS,
  AMENITY_ICONS,
  PROPERTY_TYPE_ICONS,
  REAL_AMENITIES,
} from "../listing-options";
import {
  type ApartmentSearch,
  buildUrl,
  countActiveFilters,
  MAX_BATHS,
  MAX_BEDS,
  MAX_ROOMS,
  toSearchParams,
} from "../search-params";

type FiltersDialogProps = {
  search: ApartmentSearch;
  className?: string;
};

/** "Filters" button with an active-count badge; opens the Airbnb-style filters dialog. */
export function FiltersDialog({ search, className }: FiltersDialogProps) {
  const t = useTranslations("search.filters");
  const [open, setOpen] = useState(false);
  const active = countActiveFilters(search);
  const router = useRouter();
  const [, startTransition] = useTransition();
  // "Show stays" closes the dialog first and only navigates once it has fully closed
  // (Radix calls onCloseAutoFocus after the exit animation, when aria-hidden and the
  // focus trap are gone). Navigating while it was still open rendered the new route
  // while the rest of the page was aria-hidden and focus was handed back into that
  // hidden tree, which React reported as an aria-hidden mismatch in development.
  const pendingHref = useRef<string | null>(null);

  const apply = (href: string) => {
    pendingHref.current = href;
    setOpen(false);
  };

  const onCloseAutoFocus = (event: Event) => {
    const href = pendingHref.current;
    if (!href) return;
    pendingHref.current = null;
    // The page is about to change: don't move focus back to the trigger.
    event.preventDefault();
    startTransition(() => router.push(href));
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button
          variant="outline"
          className={cn(
            "h-10 rounded-full",
            active > 0 && "border-foreground",
            className,
          )}
          aria-label={
            active > 0 ? t("buttonActive", { count: active }) : t("button")
          }
        >
          <SlidersHorizontal aria-hidden="true" />
          {t("button")}
          {active > 0 && (
            <span className="grid size-5 place-items-center rounded-full bg-foreground text-[0.7rem] font-semibold text-background tabular-nums">
              {active}
            </span>
          )}
        </Button>
      </DialogTrigger>
      <DialogContent
        className="flex max-h-[calc(100dvh-2rem)] flex-col gap-0 overflow-hidden p-0 sm:max-w-2xl"
        onCloseAutoFocus={onCloseAutoFocus}
      >
        {open && <FiltersForm initial={search} onApply={apply} />}
      </DialogContent>
    </Dialog>
  );
}

/** A round slider step giving roughly 100 positions (1, 5, 10, 50, 100, …). */
function sliderStep(span: number) {
  const raw = span / 100;
  for (const step of [1, 5, 10, 50, 100, 500, 1000])
    if (raw <= step) return step;
  return 1000;
}

function FiltersForm({
  initial,
  onApply,
}: {
  initial: ApartmentSearch;
  /** Closes the dialog, then navigates to `href`. */
  onApply: (href: string) => void;
}) {
  const t = useTranslations("search.filters");
  const tListing = useTranslations("listing");
  const locale = useLocale();
  const [draft, setDraft] = useState<ApartmentSearch>(initial);
  const priceRange = usePriceRange();
  const count = useSearchCount(draft);
  const money = useDisplayMoney();

  const patch = (next: Partial<ApartmentSearch>) =>
    setDraft((d) => ({ ...d, ...next }));

  const toggle = <T,>(values: T[], value: T) =>
    values.includes(value)
      ? values.filter((v) => v !== value)
      : [...values, value];

  // Category tags chosen in the category bar survive "Clear all".
  const onClear = () =>
    setDraft((d) => ({
      ...d,
      minPrice: undefined,
      maxPrice: undefined,
      types: [],
      bedrooms: undefined,
      beds: undefined,
      baths: undefined,
      amenities: d.amenities.filter((a) => !REAL_AMENITIES.includes(a)),
      instant: false,
      superhost: false,
    }));

  const applyFilters = () => onApply(buildUrl("/search", toSearchParams({ ...draft, page: 1 })));

  // The API filters in USD (the price range too); the slider and inputs work in the
  // visitor's display currency and convert back to USD for the URL / API.
  const apiCurrency = priceRange.data?.currency ?? BASE_CURRENCY;
  const loApi = priceRange.data?.min ?? 0;
  const hiApi = priceRange.data?.max ?? 1000;
  const display = money.convert(1, apiCurrency);
  const currency = display.currency;
  const toDisplay = (value: number) => money.convert(value, apiCurrency).amount;
  const toApi = (value: number) =>
    Math.max(
      0,
      Math.round(
        currency === apiCurrency
          ? value
          : (money.convertBack(value, apiCurrency) ?? value),
      ),
    );
  const lo = Math.floor(toDisplay(loApi));
  const hi = Math.max(lo + 1, Math.ceil(toDisplay(hiApi)));
  const step = sliderStep(hi - lo);
  const sliderValue = [
    Math.max(
      lo,
      Math.min(
        hi,
        draft.minPrice !== undefined
          ? Math.round(toDisplay(draft.minPrice))
          : lo,
      ),
    ),
    Math.max(
      lo,
      Math.min(
        hi,
        draft.maxPrice !== undefined
          ? Math.round(toDisplay(draft.maxPrice))
          : hi,
      ),
    ),
  ];
  const onSlide = ([min = lo, max = hi]: number[]) =>
    patch({
      minPrice: min <= lo ? undefined : toApi(min),
      maxPrice: max >= hi ? undefined : toApi(max),
    });

  // "$" before or "€"/"$US" after the amount, as the locale writes it.
  const [prefix, suffix] = formatPriceCompact(0, currency, locale)
    .split("0")
    .map((part) => part.trim());
  const priceInput = (
    key: "minPrice" | "maxPrice",
    label: string,
    placeholder: number,
  ) => (
    <label className="grid flex-1 gap-1 rounded-xl border px-3.5 py-2 focus-within:ring-[3px] focus-within:ring-ring/25">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="flex items-center gap-1">
        {prefix && (
          <span className="text-sm text-muted-foreground">{prefix}</span>
        )}
        <Input
          type="number"
          inputMode="numeric"
          min={0}
          value={
            draft[key] !== undefined ? Math.round(toDisplay(draft[key])) : ""
          }
          placeholder={String(placeholder)}
          onChange={(e) => {
            const value =
              e.target.value === ""
                ? undefined
                : Math.max(0, Number(e.target.value));
            patch({
              [key]:
                value !== undefined && Number.isFinite(value)
                  ? toApi(value)
                  : undefined,
            });
          }}
          className="h-7 [appearance:textfield] border-0 bg-transparent p-0 shadow-none focus-visible:ring-0 dark:bg-transparent [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
          aria-label={label}
        />
        {suffix && (
          <span className="text-sm text-muted-foreground">{suffix}</span>
        )}
      </span>
    </label>
  );

  return (
    <>
      <DialogHeader className="border-b px-6 py-4 text-center sm:text-center">
        <DialogTitle>{t("title")}</DialogTitle>
        <DialogDescription className="sr-only">
          {t("description")}
        </DialogDescription>
      </DialogHeader>

      <div className="grid gap-8 overflow-y-auto px-6 py-6">
        <section className="grid gap-4" aria-labelledby="filters-price">
          <div className="grid gap-1">
            <h3 id="filters-price" className="text-lg font-semibold">
              {t("price")}
            </h3>
            <p className="text-sm text-muted-foreground">
              {t("priceDescription")}
            </p>
            {display.converted && (
              <p
                className="text-xs text-muted-foreground"
                data-testid="price-converted-note"
              >
                {t("priceConverted", { currency, base: apiCurrency })}
              </p>
            )}
          </div>
          {priceRange.isPending ? (
            <Skeleton className="h-6 w-full" />
          ) : (
            <Slider
              min={lo}
              max={hi}
              step={step}
              minStepsBetweenThumbs={1}
              value={sliderValue}
              onValueChange={onSlide}
              thumbLabels={[t("minimum"), t("maximum")]}
              className="px-1 py-2"
            />
          )}
          <div className="flex items-center gap-3">
            {priceInput("minPrice", t("minimum"), lo)}
            <span className="text-muted-foreground" aria-hidden="true">
              –
            </span>
            {priceInput("maxPrice", t("maximum"), hi)}
          </div>
        </section>

        <Separator />

        <section className="grid gap-4" aria-labelledby="filters-types">
          <h3 id="filters-types" className="text-lg font-semibold">
            {t("propertyType")}
          </h3>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {PROPERTY_TYPES.map((type: PropertyType) => {
              const Icon = PROPERTY_TYPE_ICONS[type];
              const selected = draft.types.includes(type);
              return (
                <button
                  key={type}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => patch({ types: toggle(draft.types, type) })}
                  className={cn(
                    "flex flex-col items-start gap-6 rounded-xl border p-4 text-left text-sm font-medium transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40",
                    selected
                      ? "border-foreground bg-muted ring-1 ring-foreground"
                      : "hover:border-foreground/50",
                  )}
                >
                  <Icon className="size-6" aria-hidden="true" />
                  {tListing(`propertyTypes.${type}`)}
                </button>
              );
            })}
          </div>
        </section>

        <Separator />

        <section className="grid gap-4" aria-labelledby="filters-rooms">
          <h3 id="filters-rooms" className="text-lg font-semibold">
            {t("roomsAndBeds")}
          </h3>
          <CountStepper
            id="filter-bedrooms"
            label={t("bedrooms")}
            value={draft.bedrooms ?? 0}
            max={MAX_ROOMS}
            onChange={(v) => patch({ bedrooms: v || undefined })}
          />
          <CountStepper
            id="filter-beds"
            label={t("beds")}
            value={draft.beds ?? 0}
            max={MAX_BEDS}
            onChange={(v) => patch({ beds: v || undefined })}
          />
          <CountStepper
            id="filter-baths"
            label={t("bathrooms")}
            value={draft.baths ?? 0}
            max={MAX_BATHS}
            onChange={(v) => patch({ baths: v || undefined })}
          />
        </section>

        <Separator />

        <section className="grid gap-6" aria-labelledby="filters-amenities">
          <h3 id="filters-amenities" className="text-lg font-semibold">
            {t("amenities")}
          </h3>
          {AMENITY_GROUPS.map((group) => (
            <fieldset key={group.key} className="grid gap-3">
              <legend className="mb-3 text-sm font-semibold">
                {tListing(`amenityGroups.${group.key}`)}
              </legend>
              <div className="grid gap-3 sm:grid-cols-2">
                {group.amenities.map((amenity: Amenity) => {
                  const Icon = AMENITY_ICONS[amenity];
                  const id = `filter-amenity-${amenity}`;
                  return (
                    <label
                      key={amenity}
                      htmlFor={id}
                      className="flex cursor-pointer items-center gap-3 text-sm"
                    >
                      <Checkbox
                        id={id}
                        checked={draft.amenities.includes(amenity)}
                        onCheckedChange={() =>
                          patch({ amenities: toggle(draft.amenities, amenity) })
                        }
                      />
                      <Icon
                        className="size-5 text-muted-foreground"
                        aria-hidden="true"
                      />
                      {tListing(`amenities.${amenity}`)}
                    </label>
                  );
                })}
              </div>
            </fieldset>
          ))}
        </section>

        <Separator />

        <section className="grid gap-5" aria-labelledby="filters-options">
          <h3 id="filters-options" className="text-lg font-semibold">
            {t("bookingOptions")}
          </h3>
          <ToggleRow
            id="filter-instant"
            icon={Zap}
            label={t("instantBook")}
            description={t("instantBookDescription")}
            checked={draft.instant}
            onChange={(instant) => patch({ instant })}
          />
          <ToggleRow
            id="filter-superhost"
            icon={Award}
            label={t("superhost")}
            description={t("superhostDescription")}
            checked={draft.superhost}
            onChange={(superhost) => patch({ superhost })}
          />
        </section>
      </div>

      <div className="flex items-center justify-between gap-3 border-t bg-popover px-6 py-4">
        <Button
          type="button"
          variant="link"
          className="px-0 text-foreground underline"
          onClick={onClear}
        >
          {t("clearAll")}
        </Button>
        <Button
          size="lg"
          className="min-w-40"
          onClick={applyFilters}
          data-testid="filters-apply"
        >
          {count.isFetching && (
            <Loader2 className="animate-spin" aria-hidden="true" />
          )}
          <span aria-live="polite">
            {count.data === undefined
              ? t("showStays")
              : t("showCount", { count: count.data })}
          </span>
        </Button>
      </div>
    </>
  );
}

function CountStepper({
  id,
  label,
  value,
  max,
  onChange,
}: {
  id: string;
  label: string;
  value: number;
  max: number;
  onChange: (value: number) => void;
}) {
  const t = useTranslations("common");
  const tFilters = useTranslations("search.filters");
  return (
    <div
      className="flex items-center justify-between gap-4"
      role="group"
      aria-labelledby={`${id}-label`}
    >
      <span id={`${id}-label`} className="text-sm">
        {label}
      </span>
      <div className="flex items-center gap-3">
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.max(0, value - 1))}
          disabled={value <= 0}
          aria-label={t("decrease", { label })}
        >
          <Minus />
        </Button>
        <output
          aria-live="polite"
          className="w-10 text-center text-sm font-semibold tabular-nums"
        >
          {value === 0 ? tFilters("any") : `${value}+`}
        </output>
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.min(max, value + 1))}
          disabled={value >= max}
          aria-label={t("increase", { label })}
        >
          <Plus />
        </Button>
      </div>
    </div>
  );
}

function ToggleRow({
  id,
  icon: Icon,
  label,
  description,
  checked,
  onChange,
}: {
  id: string;
  icon: typeof Zap;
  label: string;
  description: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-4">
      <label htmlFor={id} className="flex cursor-pointer items-start gap-3">
        <Icon
          className="mt-0.5 size-5 shrink-0 text-muted-foreground"
          aria-hidden="true"
        />
        <span className="grid gap-0.5">
          <span className="text-sm font-medium">{label}</span>
          <span className="text-xs text-muted-foreground">{description}</span>
        </span>
      </label>
      <Switch id={id} checked={checked} onCheckedChange={onChange} />
    </div>
  );
}

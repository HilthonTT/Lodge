"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, ArrowRight, Check, LocateFixed, Loader2, MapPin, Pencil, RotateCcw } from "lucide-react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from "react";
import { Controller, type FieldErrors, type FieldPath, useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";

import { RemoteImage } from "@/components/shared/remote-image";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import {
  AMENITY_GROUPS,
  AMENITY_ICONS,
  LISTING_TAGS,
  PROPERTY_TYPE_ICONS,
} from "@/features/apartments/listing-options";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { getBrowserHttp } from "@/lib/api/browser";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import { type Amenity, type HostListing, PROPERTY_TYPES } from "@/types/api";

import { useHostListing, useSaveListing } from "../../hooks/use-hosting";
import {
  createListingSchema,
  EMPTY_LISTING,
  LIMITS,
  type ListingFormValues,
  SERVER_CODES,
  STEP_FIELDS,
  type StepKey,
  STEPS,
  toListingRequest,
} from "../../schemas/listing";
import { HostingError } from "../hosting-gate";
import { CancellationPolicyPicker } from "./cancellation-policy-picker";
import { NumberStepper, SwitchRow } from "./fields";
import { PhotosStep } from "./photos-step";
import type { PinMapProps } from "./pin-map";

const PinMap = dynamic<PinMapProps>(() => import("./pin-map"), {
  ssr: false,
  loading: () => <Skeleton className="size-full rounded-none" />,
});

/* ------------------------------------------------------------------ */
/* Draft storage (sessionStorage, per tab)                             */
/* ------------------------------------------------------------------ */

type Draft = { values: ListingFormValues; step: number };

const draftKey = (listingId?: string) => `lodge:listing-draft:${listingId ?? "new"}`;

function readDraftRaw(key: string): string | null {
  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function parseDraft(raw: string | null): Draft | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Draft;
    const v = { ...EMPTY_LISTING, ...parsed.values, address: { ...EMPTY_LISTING.address, ...parsed.values?.address } };
    // JSON turns NaN into null: restore "empty" numbers.
    for (const k of ["latitude", "longitude", "price"] as const) if (v[k] === null) v[k] = Number.NaN;
    return { values: v, step: Math.min(Math.max(0, Number(parsed.step) || 0), STEPS.length - 1) };
  } catch {
    return null;
  }
}

function clearDraft(key: string) {
  try {
    window.sessionStorage.removeItem(key);
  } catch {
    // Storage blocked: nothing to clear.
  }
}

const noopSubscribe = () => () => {};

function fromListing(listing: HostListing): ListingFormValues {
  return {
    name: listing.name,
    description: listing.description,
    propertyType: listing.propertyType,
    address: {
      country: listing.address.country ?? "",
      state: listing.address.state ?? "",
      zipCode: listing.address.zipCode ?? "",
      city: listing.address.city ?? "",
      street: listing.address.street ?? "",
    },
    latitude: listing.latitude,
    longitude: listing.longitude,
    bedrooms: listing.bedrooms,
    beds: listing.beds,
    bathrooms: listing.bathrooms,
    maximumGuestCount: listing.maximumGuestCount,
    amenities: listing.amenities,
    photos: listing.images.map((image) => ({ id: image.id, url: image.url })),
    price: listing.price,
    currency: listing.currency === "EUR" ? "EUR" : "USD",
    cleaningFee: listing.cleaningFee,
    weekendPrice: listing.weekendPrice,
    weeklyDiscountPercent: listing.weeklyDiscountPercent,
    monthlyDiscountPercent: listing.monthlyDiscountPercent,
    minNights: listing.minNights,
    maxNights: listing.maxNights,
    instantBook: listing.instantBook,
    checkInTime: listing.checkInTime,
    checkOutTime: listing.checkOutTime,
    petsAllowed: listing.petsAllowed,
    smokingAllowed: listing.smokingAllowed,
    partiesAllowed: listing.partiesAllowed,
    houseRules: listing.houseRules ?? "",
    checkInInstructions: listing.checkInInstructions ?? "",
    cancellationPolicy: listing.cancellationPolicy ?? "Moderate",
    isListed: listing.isListed,
  };
}

/* ------------------------------------------------------------------ */
/* Loader                                                              */
/* ------------------------------------------------------------------ */

export function ListingWizard({ listingId }: { listingId?: string }) {
  const t = useTranslations("hosting.wizard");
  const listing = useHostListing(listingId);
  const key = draftKey(listingId);
  // undefined on the server; the stored draft (or null) once hydrated.
  const raw = useSyncExternalStore(noopSubscribe, () => readDraftRaw(key), () => undefined);

  if (raw === undefined || (listingId && listing.isPending)) return <WizardSkeleton />;
  if (listingId && listing.isError)
    return <HostingError error={listing.error} title={t("loadError")} onRetry={() => listing.refetch()} />;

  const original = listing.data ? fromListing(listing.data) : EMPTY_LISTING;
  const draft = parseDraft(raw);

  return (
    <WizardForm
      listingId={listingId}
      listingName={listing.data?.name}
      storageKey={key}
      original={original}
      initial={draft?.values ?? original}
      initialStep={draft?.step ?? 0}
      restored={Boolean(draft)}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Form                                                                */
/* ------------------------------------------------------------------ */

type WizardFormProps = {
  listingId?: string;
  listingName?: string;
  storageKey: string;
  original: ListingFormValues;
  initial: ListingFormValues;
  initialStep: number;
  restored: boolean;
};

const numberValue = (v: unknown) => (v === "" || v === null || v === undefined ? Number.NaN : Number(v));
const nullableNumber = (v: unknown) => (v === "" || v === null || v === undefined ? null : Number(v));

function WizardForm({ listingId, listingName, storageKey, original, initial, initialStep, restored }: WizardFormProps) {
  const t = useTranslations("hosting.wizard");
  const tErrors = useTranslations("hosting.wizard.errors");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const schema = useMemo(() => createListingSchema(tErrors), [tErrors]);
  const form = useForm<ListingFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initial,
    mode: "onTouched",
  });
  const [step, setStep] = useState(initialStep);
  const [visited, setVisited] = useState(listingId ? STEPS.length - 1 : initialStep);
  const [showRestored, setShowRestored] = useState(restored);
  const save = useSaveListing(listingId);
  const values = useWatch({ control: form.control }) as ListingFormValues;
  const stepKey: StepKey = STEPS[step] ?? "basics";

  // Keep a draft of every change for this tab (nothing to keep until something changed).
  const isDirty = form.formState.isDirty;
  useEffect(() => {
    if (save.isSuccess || (!isDirty && step === 0)) return;
    try {
      window.sessionStorage.setItem(storageKey, JSON.stringify({ values, step } satisfies Draft));
    } catch {
      // Storage blocked or full: the wizard still works, just without a draft.
    }
  }, [values, step, storageKey, save.isSuccess, isDirty]);

  const goTo = useCallback((index: number) => {
    setStep(index);
    setVisited((v) => Math.max(v, index));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, []);

  const firstInvalidStep = (errors: FieldErrors<ListingFormValues>) =>
    STEPS.findIndex((s) =>
      STEP_FIELDS[s].some((field) => {
        const [head, tail] = field.split(".") as [keyof ListingFormValues, string | undefined];
        const error = errors[head];
        return tail ? Boolean((error as Record<string, unknown> | undefined)?.[tail]) : Boolean(error);
      }),
    );

  const onNext = async () => {
    const ok = await form.trigger(STEP_FIELDS[stepKey], { shouldFocus: true });
    if (ok) goTo(step + 1);
  };

  const onSubmit = form.handleSubmit(
    (data) =>
      save.mutate(toListingRequest(data), {
        onSuccess: (id) => {
          clearDraft(storageKey);
          toast.success(listingId ? t("updated") : t("created"), { description: data.name });
          router.push(listingId ? "/hosting/listings" : `/hosting/listings?created=${id}`);
        },
        onError: (error) => {
          const code = Object.keys(SERVER_CODES).find((c) => hasErrorCode(error, c));
          if (code) {
            const target = SERVER_CODES[code]!;
            form.setError(target.field, { type: "server", message: t(`server.${target.key}`) });
            goTo(STEPS.indexOf(target.step));
            toast.error(t("fixStep", { step: t(`steps.${target.step}.title`) }));
          } else {
            toast.error(t("saveError"), { description: errorMessage(error) });
          }
        },
      }),
    (errors) => {
      const index = firstInvalidStep(errors);
      if (index >= 0) {
        goTo(index);
        toast.error(t("fixStep", { step: t(`steps.${STEPS[index]!}.title`) }));
      }
    },
  );

  const discardDraft = () => {
    clearDraft(storageKey);
    form.reset(original);
    setShowRestored(false);
    goTo(0);
  };

  const err = (path: FieldPath<ListingFormValues>) => {
    const parts = path.split(".");
    let node: unknown = form.formState.errors;
    for (const part of parts) node = (node as Record<string, unknown> | undefined)?.[part];
    return (node as { message?: string } | undefined)?.message;
  };

  const isLast = step === STEPS.length - 1;
  const progress = ((step + 1) / STEPS.length) * 100;

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-8">
      <header className="grid gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="grid gap-1">
            <p className="text-sm text-muted-foreground">
              <Link href="/hosting/listings" className="underline-offset-4 hover:underline">
                {t("breadcrumb")}
              </Link>
            </p>
            <h1 className="text-2xl font-semibold sm:text-3xl">
              {listingId ? t("editTitle", { name: listingName ?? "" }) : t("newTitle")}
            </h1>
          </div>
          <p className="text-sm font-medium text-muted-foreground" aria-live="polite">
            {t("stepOf", { step: String(step + 1), total: String(STEPS.length) })}
          </p>
        </div>
        <div className="h-1.5 overflow-hidden rounded-full bg-muted" aria-hidden="true">
          <div className="h-full rounded-full bg-primary transition-[width] duration-300" style={{ width: `${progress}%` }} />
        </div>
        <ol className="-mx-1 flex gap-1 overflow-x-auto px-1 pb-1" aria-label={t("stepsLabel")}>
          {STEPS.map((s, index) => (
            <li key={s}>
              <button
                type="button"
                onClick={() => index <= visited && goTo(index)}
                disabled={index > visited}
                aria-current={index === step ? "step" : undefined}
                className={cn(
                  "inline-flex h-8 shrink-0 items-center gap-1.5 rounded-full px-3 text-xs font-medium whitespace-nowrap transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                  index === step
                    ? "bg-foreground text-background"
                    : index <= visited
                      ? "bg-muted hover:bg-muted/70"
                      : "text-muted-foreground/60",
                )}
              >
                {index < step ? <Check className="size-3.5" aria-hidden="true" /> : <span>{index + 1}.</span>}
                {t(`steps.${s}.title`)}
              </button>
            </li>
          ))}
        </ol>
      </header>

      {showRestored && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border bg-accent/50 px-4 py-3 text-sm">
          <p>{t("draftRestored")}</p>
          <Button type="button" variant="ghost" size="sm" onClick={discardDraft}>
            <RotateCcw aria-hidden="true" /> {t("discardDraft")}
          </Button>
        </div>
      )}

      <section aria-labelledby="step-heading" className="grid gap-6 rounded-3xl border bg-card p-5 shadow-card sm:p-8">
        <div className="grid gap-1">
          <h2 id="step-heading" className="text-xl font-semibold">
            {t(`steps.${stepKey}.title`)}
          </h2>
          <p className="text-sm text-muted-foreground">{t(`steps.${stepKey}.description`)}</p>
        </div>

        {stepKey === "basics" && (
          <div className="grid gap-6">
            <Field id="listing-name" label={t("fields.name")} error={err("name")}>
              {(control) => (
                <Input {...control} {...form.register("name")} maxLength={LIMITS.name} placeholder={t("fields.namePlaceholder")} />
              )}
            </Field>
            <Field
              id="listing-description"
              label={t("fields.description")}
              error={err("description")}
              description={t("charactersLeft", { count: LIMITS.description - (values.description?.length ?? 0) })}
            >
              {(control) => (
                <Textarea
                  {...control}
                  {...form.register("description")}
                  rows={6}
                  maxLength={LIMITS.description}
                  placeholder={t("fields.descriptionPlaceholder")}
                />
              )}
            </Field>
            <fieldset className="grid gap-3">
              <legend className="mb-3 text-sm font-medium">{t("fields.propertyType")}</legend>
              <Controller
                control={form.control}
                name="propertyType"
                render={({ field }) => (
                  <div role="radiogroup" aria-label={t("fields.propertyType")} className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                    {PROPERTY_TYPES.map((type) => {
                      const Icon = PROPERTY_TYPE_ICONS[type];
                      const active = field.value === type;
                      return (
                        <button
                          key={type}
                          type="button"
                          role="radio"
                          aria-checked={active}
                          onClick={() => field.onChange(type)}
                          className={cn(
                            "flex items-center gap-3 rounded-xl border p-4 text-left text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                            active ? "border-foreground bg-muted/70 ring-1 ring-foreground" : "hover:bg-muted/50",
                          )}
                        >
                          <Icon className="size-5 shrink-0" aria-hidden="true" />
                          <ListingLabel kind="propertyTypes" value={type} />
                        </button>
                      );
                    })}
                  </div>
                )}
              />
              {err("propertyType") && <p className="text-xs font-medium text-destructive">{err("propertyType")}</p>}
            </fieldset>
          </div>
        )}

        {stepKey === "location" && <LocationStep form={form} err={err} />}

        {stepKey === "details" && (
          <div className="grid gap-8">
            <div className="divide-y">
              {(
                [
                  ["maximumGuestCount", LIMITS.guests, 1],
                  ["bedrooms", LIMITS.bedrooms, 1],
                  ["beds", LIMITS.beds, 1],
                  ["bathrooms", LIMITS.bathrooms, 0.5],
                ] as const
              ).map(([name, [min, max], stepSize]) => (
                <Controller
                  key={name}
                  control={form.control}
                  name={name}
                  render={({ field }) => (
                    <NumberStepper
                      id={`listing-${name}`}
                      label={t(`fields.${name}`)}
                      value={field.value}
                      min={min}
                      max={max}
                      step={stepSize}
                      onChange={field.onChange}
                    />
                  )}
                />
              ))}
            </div>
            {err("bedrooms") && <p className="text-xs font-medium text-destructive">{err("bedrooms")}</p>}
            <Controller
              control={form.control}
              name="amenities"
              render={({ field }) => {
                const toggle = (amenity: Amenity, on: boolean) =>
                  field.onChange(on ? [...field.value, amenity] : field.value.filter((a) => a !== amenity));
                const groups = [
                  ...AMENITY_GROUPS.map((g) => ({ key: g.key, amenities: g.amenities as readonly Amenity[] })),
                  { key: "categories" as const, amenities: LISTING_TAGS },
                ];
                return (
                  <div className="grid gap-6">
                    {groups.map((group) => (
                      <fieldset key={group.key} className="grid gap-3">
                        <legend className="mb-3 font-semibold">
                          <GroupLabel group={group.key} />
                        </legend>
                        <div className="grid gap-2 sm:grid-cols-2">
                          {group.amenities.map((amenity) => {
                            const Icon = AMENITY_ICONS[amenity];
                            const id = `amenity-${amenity}`;
                            return (
                              <label
                                key={amenity}
                                htmlFor={id}
                                className="flex cursor-pointer items-center gap-3 rounded-xl border px-4 py-3 text-sm transition-colors hover:bg-muted/50 has-[[data-state=checked]]:border-foreground"
                              >
                                <Checkbox
                                  id={id}
                                  checked={field.value.includes(amenity)}
                                  onCheckedChange={(on) => toggle(amenity, on === true)}
                                />
                                <Icon className="size-4.5 text-muted-foreground" aria-hidden="true" />
                                <ListingLabel kind="amenities" value={amenity} />
                              </label>
                            );
                          })}
                        </div>
                      </fieldset>
                    ))}
                  </div>
                );
              }}
            />
          </div>
        )}

        {stepKey === "photos" && (
          <Controller
            control={form.control}
            name="photos"
            render={({ field, fieldState }) => (
              <PhotosStep
                photos={field.value}
                onChange={(photos) => {
                  field.onChange(photos);
                  if (photos.length > 0) form.clearErrors("photos");
                }}
                error={fieldState.error?.message}
              />
            )}
          />
        )}

        {stepKey === "pricing" && (
          <div className="grid gap-6">
            <div className="grid gap-6 sm:grid-cols-[minmax(0,1fr)_10rem]">
              <Field id="listing-price" label={t("fields.price")} error={err("price")} description={t("fields.priceHint")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="decimal" min={1} step="0.01" {...form.register("price", { setValueAs: numberValue })} />
                )}
              </Field>
              <Field id="listing-currency" label={t("fields.currency")} error={err("currency")}>
                {(control) => (
                  <select
                    {...control}
                    {...form.register("currency")}
                    className="h-11 w-full rounded-lg border border-input bg-background px-3 text-sm shadow-xs outline-none focus-visible:ring-[3px] focus-visible:ring-ring/25 dark:bg-input/20"
                  >
                    <option value="USD">USD ($)</option>
                    <option value="EUR">EUR (€)</option>
                  </select>
                )}
              </Field>
            </div>
            <div className="grid gap-6 sm:grid-cols-2">
              <Field id="listing-weekend" label={t("fields.weekendPrice")} error={err("weekendPrice")} description={t("fields.weekendPriceHint")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="decimal" min={1} step="0.01" {...form.register("weekendPrice", { setValueAs: nullableNumber })} />
                )}
              </Field>
              <Field id="listing-cleaning" label={t("fields.cleaningFee")} error={err("cleaningFee")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="decimal" min={0} step="0.01" {...form.register("cleaningFee", { setValueAs: numberValue })} />
                )}
              </Field>
              <Field id="listing-weekly" label={t("fields.weeklyDiscount")} error={err("weeklyDiscountPercent")} description={t("fields.weeklyHint")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="numeric" min={0} max={90} {...form.register("weeklyDiscountPercent", { setValueAs: numberValue })} />
                )}
              </Field>
              <Field id="listing-monthly" label={t("fields.monthlyDiscount")} error={err("monthlyDiscountPercent")} description={t("fields.monthlyHint")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="numeric" min={0} max={90} {...form.register("monthlyDiscountPercent", { setValueAs: numberValue })} />
                )}
              </Field>
              <Field id="listing-min-nights" label={t("fields.minNights")} error={err("minNights")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="numeric" min={1} max={365} {...form.register("minNights", { setValueAs: numberValue, deps: ["maxNights"] })} />
                )}
              </Field>
              <Field id="listing-max-nights" label={t("fields.maxNights")} error={err("maxNights")}>
                {(control) => (
                  <Input {...control} type="number" inputMode="numeric" min={1} max={365} {...form.register("maxNights", { setValueAs: numberValue })} />
                )}
              </Field>
            </div>
            <Separator />
            <Controller
              control={form.control}
              name="instantBook"
              render={({ field }) => (
                <SwitchRow
                  id="listing-instant"
                  label={t("fields.instantBook")}
                  description={field.value ? t("fields.instantBookOn") : t("fields.instantBookOff")}
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              )}
            />
          </div>
        )}

        {stepKey === "rules" && (
          <div className="grid gap-6">
            <div className="grid gap-6 sm:grid-cols-2">
              <Field id="listing-checkin" label={t("fields.checkInTime")} error={err("checkInTime")}>
                {(control) => <Input {...control} type="time" {...form.register("checkInTime")} />}
              </Field>
              <Field id="listing-checkout" label={t("fields.checkOutTime")} error={err("checkOutTime")}>
                {(control) => <Input {...control} type="time" {...form.register("checkOutTime")} />}
              </Field>
            </div>
            <div className="divide-y">
              {(["petsAllowed", "smokingAllowed", "partiesAllowed"] as const).map((name) => (
                <Controller
                  key={name}
                  control={form.control}
                  name={name}
                  render={({ field }) => (
                    <SwitchRow id={`listing-${name}`} label={t(`fields.${name}`)} checked={field.value} onCheckedChange={field.onChange} />
                  )}
                />
              ))}
            </div>
            <Field id="listing-rules" label={t("fields.houseRules")} error={err("houseRules")} description={t("fields.houseRulesHint")}>
              {(control) => (
                <Textarea {...control} rows={4} maxLength={LIMITS.houseRules} {...form.register("houseRules")} placeholder={t("fields.houseRulesPlaceholder")} />
              )}
            </Field>
            <Field
              id="listing-instructions"
              label={t("fields.checkInInstructions")}
              error={err("checkInInstructions")}
              description={t("fields.checkInInstructionsHint")}
            >
              {(control) => (
                <Textarea
                  {...control}
                  rows={4}
                  maxLength={LIMITS.checkInInstructions}
                  {...form.register("checkInInstructions")}
                  placeholder={t("fields.checkInInstructionsPlaceholder")}
                />
              )}
            </Field>
            <Separator />
            <Controller
              control={form.control}
              name="cancellationPolicy"
              render={({ field }) => (
                <CancellationPolicyPicker value={field.value} onChange={field.onChange} error={err("cancellationPolicy")} />
              )}
            />
          </div>
        )}

        {stepKey === "review" && (
          <ReviewStep values={values} onEdit={(s) => goTo(STEPS.indexOf(s))} isEdit={Boolean(listingId)} form={form} />
        )}
      </section>

      <div className="sticky bottom-0 z-20 -mx-4 flex items-center justify-between gap-3 border-t bg-background/95 px-4 py-4 backdrop-blur sm:static sm:mx-0 sm:border-0 sm:bg-transparent sm:px-0 sm:py-0">
        <Button type="button" variant="ghost" onClick={() => goTo(step - 1)} disabled={step === 0}>
          <ArrowLeft aria-hidden="true" /> {t("back")}
        </Button>
        {isLast ? (
          // Distinct keys: reusing the "Next" <button> as type="submit" mid-click would submit the form.
          <Button key="submit" type="submit" size="lg" className="rounded-full" disabled={save.isPending || save.isSuccess}>
            {save.isPending || save.isSuccess ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Check aria-hidden="true" />}
            {listingId ? t("saveChanges") : values.isListed ? t("publish") : t("saveUnlisted")}
          </Button>
        ) : (
          <Button key="next" type="button" size="lg" className="rounded-full" onClick={onNext}>
            {t("next")} <ArrowRight aria-hidden="true" />
          </Button>
        )}
      </div>
    </form>
  );
}

type WizardFormApi = ReturnType<typeof useForm<ListingFormValues>>;

function ListingLabel({ kind, value }: { kind: "amenities" | "propertyTypes"; value: string }) {
  const t = useTranslations("listing");
  return <>{kind === "amenities" ? t(`amenities.${value as Amenity}`) : t(`propertyTypes.${value as (typeof PROPERTY_TYPES)[number]}`)}</>;
}

function GroupLabel({ group }: { group: "essentials" | "features" | "location" | "categories" }) {
  const t = useTranslations("listing.amenityGroups");
  return <>{t(group)}</>;
}

/* ------------------------------------------------------------------ */
/* Location                                                            */
/* ------------------------------------------------------------------ */

function LocationStep({ form, err }: { form: WizardFormApi; err: (path: FieldPath<ListingFormValues>) => string | undefined }) {
  const t = useTranslations("hosting.wizard");
  const [lat, lng, city, country] = useWatch({
    control: form.control,
    name: ["latitude", "longitude", "address.city", "address.country"],
  });
  const hasPin = Number.isFinite(lat) && Number.isFinite(lng);
  const [focus, setFocus] = useState<PinMapProps["focus"]>(null);
  const [locating, setLocating] = useState(false);
  const [locateFailed, setLocateFailed] = useState(false);

  const onPin = useCallback(
    ({ lat: nextLat, lng: nextLng }: { lat: number; lng: number }) => {
      form.setValue("latitude", nextLat, { shouldDirty: true, shouldValidate: true });
      form.setValue("longitude", nextLng, { shouldDirty: true, shouldValidate: true });
    },
    [form],
  );

  // Center the map on the city the host typed, via the destination suggestions.
  const locate = async () => {
    const query = [city, country].filter(Boolean).join(" ").trim() || city;
    if (!query || query.length < 2) return;
    setLocating(true);
    setLocateFailed(false);
    try {
      const results = await apartmentsApi.suggestLocations(getBrowserHttp(), (city || country).trim());
      const match =
        results.find((r) => r.city.toLowerCase() === city.trim().toLowerCase()) ??
        results.find((r) => r.country.toLowerCase() === country.trim().toLowerCase()) ??
        results[0];
      if (match) setFocus({ lat: match.latitude, lng: match.longitude, zoom: 12 });
      else setLocateFailed(true);
    } catch {
      setLocateFailed(true);
    } finally {
      setLocating(false);
    }
  };

  const fields = [
    ["street", "address.street", "street-address"],
    ["city", "address.city", "address-level2"],
    ["state", "address.state", "address-level1"],
    ["zipCode", "address.zipCode", "postal-code"],
    ["country", "address.country", "country-name"],
  ] as const;

  return (
    <div className="grid gap-6">
      <div className="grid gap-6 sm:grid-cols-2">
        {fields.map(([label, name, autoComplete]) => (
          <Field
            key={name}
            id={`listing-${label}`}
            label={t(`fields.${label}`)}
            error={err(name)}
            className={label === "street" ? "sm:col-span-2" : undefined}
          >
            {(control) => <Input {...control} autoComplete={autoComplete} maxLength={LIMITS.address} {...form.register(name)} />}
          </Field>
        ))}
      </div>
      <div className="grid gap-3">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="grid gap-0.5">
            <p className="font-medium">{t("location.pinTitle")}</p>
            <p className="text-sm text-muted-foreground">{t("location.pinHint")}</p>
          </div>
          <Button type="button" variant="outline" size="sm" onClick={locate} disabled={locating || !(city || country)}>
            {locating ? <Loader2 className="animate-spin" aria-hidden="true" /> : <LocateFixed aria-hidden="true" />}
            {t("location.findCity")}
          </Button>
        </div>
        <div
          className={cn(
            "relative isolate h-80 overflow-hidden rounded-2xl border bg-muted sm:h-96",
            err("latitude") && "border-destructive",
          )}
          data-testid="pin-map"
        >
          <PinMap value={hasPin ? { lat, lng } : null} onChange={onPin} focus={focus} />
        </div>
        <p
          className={cn("flex items-center gap-1.5 text-sm", err("latitude") ? "font-medium text-destructive" : "text-muted-foreground")}
          aria-live="polite"
        >
          <MapPin className="size-4" aria-hidden="true" />
          {hasPin
            ? t("location.pinned", { lat: lat.toFixed(5), lng: lng.toFixed(5) })
            : (err("latitude") ?? (locateFailed ? t("location.notFound") : t("location.noPin")))}
        </p>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Review                                                              */
/* ------------------------------------------------------------------ */

function ReviewStep({
  values,
  onEdit,
  isEdit,
  form,
}: {
  values: ListingFormValues;
  onEdit: (step: StepKey) => void;
  isEdit: boolean;
  form: WizardFormApi;
}) {
  const t = useTranslations("hosting.wizard");
  const tCommon = useTranslations("common");
  const tListing = useTranslations("listing");
  const tPolicy = useTranslations("listing.cancellationPolicies");
  const locale = useLocale();
  const yes = (v: boolean) => (v ? t("review.yes") : t("review.no"));
  const money = (v: number | null) => (v === null || !Number.isFinite(v) ? "—" : formatPrice(v, values.currency, locale));

  const sections: { step: StepKey; rows: [string, React.ReactNode][] }[] = [
    {
      step: "basics",
      rows: [
        [t("fields.name"), values.name],
        [t("fields.propertyType"), tListing(`propertyTypes.${values.propertyType}`)],
        [t("fields.description"), <span key="d" className="line-clamp-3 whitespace-pre-line">{values.description}</span>],
      ],
    },
    {
      step: "location",
      rows: [
        [
          t("review.address"),
          [values.address.street, [values.address.zipCode, values.address.city].filter(Boolean).join(" "), values.address.state, values.address.country]
            .filter(Boolean)
            .join(", "),
        ],
        [t("review.pin"), Number.isFinite(values.latitude) ? `${values.latitude.toFixed(4)}, ${values.longitude.toFixed(4)}` : "—"],
      ],
    },
    {
      step: "details",
      rows: [
        [
          t("review.capacity"),
          [
            tCommon("guests", { count: values.maximumGuestCount }),
            tCommon("bedrooms", { count: values.bedrooms }),
            tCommon("beds", { count: values.beds }),
            tCommon("baths", { count: values.bathrooms }),
          ].join(" · "),
        ],
        [
          t("review.amenities"),
          values.amenities.length ? values.amenities.map((a) => tListing(`amenities.${a}`)).join(", ") : "—",
        ],
      ],
    },
    {
      step: "pricing",
      rows: [
        [t("fields.price"), money(values.price)],
        [t("fields.weekendPrice"), money(values.weekendPrice)],
        [t("fields.cleaningFee"), money(values.cleaningFee)],
        [t("review.discounts"), t("review.discountValues", { weekly: String(values.weeklyDiscountPercent), monthly: String(values.monthlyDiscountPercent) })],
        [t("review.stay"), t("review.stayValues", { min: String(values.minNights), max: String(values.maxNights) })],
        [t("fields.instantBook"), yes(values.instantBook)],
      ],
    },
    {
      step: "rules",
      rows: [
        [t("review.times"), t("review.timeValues", { checkIn: values.checkInTime, checkOut: values.checkOutTime })],
        [t("fields.petsAllowed"), yes(values.petsAllowed)],
        [t("fields.smokingAllowed"), yes(values.smokingAllowed)],
        [t("fields.partiesAllowed"), yes(values.partiesAllowed)],
        [t("fields.checkInInstructions"), values.checkInInstructions ? t("review.provided") : "—"],
        [t("fields.cancellationPolicy"), tPolicy(`${values.cancellationPolicy}.name`)],
      ],
    },
  ];

  return (
    <div className="grid gap-8">
      <div className="grid gap-4 sm:grid-cols-[14rem_minmax(0,1fr)] sm:items-start">
        <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted">
          <RemoteImage src={values.photos[0]?.url} alt="" fill sizes="224px" className="object-cover" />
        </div>
        <div className="grid gap-2">
          <p className="text-lg font-semibold">{values.name || "—"}</p>
          <p className="text-sm text-muted-foreground">{t("review.photos", { count: values.photos.length })}</p>
          <div className="flex flex-wrap gap-1.5">
            <Badge variant={values.instantBook ? "success" : "info"}>
              {values.instantBook ? t("review.instant") : t("review.request")}
            </Badge>
          </div>
          <Button type="button" variant="link" className="justify-self-start px-0" onClick={() => onEdit("photos")}>
            <Pencil aria-hidden="true" /> {t("review.editPhotos")}
          </Button>
        </div>
      </div>
      <dl className="grid gap-6">
        {sections.map((section) => (
          <div key={section.step} className="grid gap-3 rounded-2xl border p-5">
            <div className="flex items-center justify-between gap-2">
              <dt className="font-semibold">{t(`steps.${section.step}.title`)}</dt>
              <Button type="button" variant="ghost" size="sm" onClick={() => onEdit(section.step)}>
                <Pencil aria-hidden="true" /> {tCommon("edit")}
              </Button>
            </div>
            <dd>
              <dl className="grid gap-2 text-sm">
                {section.rows.map(([label, value]) => (
                  <div key={label} className="grid gap-0.5 sm:grid-cols-[11rem_minmax(0,1fr)] sm:gap-4">
                    <dt className="text-muted-foreground">{label}</dt>
                    <dd className="break-words">{value || "—"}</dd>
                  </div>
                ))}
              </dl>
            </dd>
          </div>
        ))}
      </dl>
      {!isEdit && (
        <Controller
          control={form.control}
          name="isListed"
          render={({ field }) => (
            <div className="rounded-2xl border bg-muted/40 px-5">
              <SwitchRow
                id="listing-publish"
                label={t("review.publishNow")}
                description={field.value ? t("review.publishOn") : t("review.publishOff")}
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </div>
          )}
        />
      )}
    </div>
  );
}

function WizardSkeleton() {
  return (
    <div className="grid gap-6" role="status">
      <Skeleton className="h-9 w-72" />
      <Skeleton className="h-1.5 w-full" />
      <Skeleton className="h-96 rounded-3xl" />
    </div>
  );
}

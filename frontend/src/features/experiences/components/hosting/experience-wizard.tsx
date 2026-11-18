"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, ArrowRight, Check, Loader2, LocateFixed, MapPin, Pencil, Plus, X } from "lucide-react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useCallback, useMemo, useState } from "react";
import { Controller, type FieldErrors, useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { HostingError } from "@/features/hosting/components/hosting-gate";
import { NumberStepper, SwitchRow } from "@/features/hosting/components/wizard/fields";
import { PhotosStep } from "@/features/hosting/components/wizard/photos-step";
import type { PinMapProps } from "@/features/hosting/components/wizard/pin-map";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { getBrowserHttp } from "@/lib/api/browser";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import { EXPERIENCE_CATEGORIES } from "@/types/api";

import { CATEGORY_THEME, ExperienceImage, LANGUAGE_CODES, useFormatDuration, useLanguageName } from "../../experience-options";
import { useHostExperience, useSaveExperience } from "../../hooks/use-experiences";
import {
  createExperienceSchema,
  EMPTY_EXPERIENCE,
  EXPERIENCE_LIMITS,
  EXPERIENCE_SERVER_CODES,
  EXPERIENCE_STEP_FIELDS,
  EXPERIENCE_STEPS,
  type ExperienceFormValues,
  type ExperienceStep,
  fromExperience,
  toExperienceRequest,
} from "../../schemas/experience";

const PinMap = dynamic<PinMapProps>(() => import("@/features/hosting/components/wizard/pin-map"), {
  ssr: false,
  loading: () => <Skeleton className="size-full rounded-none" />,
});

type FormApi = ReturnType<typeof useForm<ExperienceFormValues>>;

export function ExperienceWizard({ experienceId }: { experienceId?: string }) {
  const t = useTranslations("experiences.hosting.wizard");
  const experience = useHostExperience(experienceId);

  if (experienceId && experience.isPending) {
    return (
      <div className="grid gap-6" role="status">
        <Skeleton className="h-9 w-72" />
        <Skeleton className="h-96 rounded-3xl" />
      </div>
    );
  }
  if (experienceId && experience.isError) {
    return <HostingError error={experience.error} title={t("loadError")} onRetry={() => experience.refetch()} />;
  }
  return (
    <WizardForm
      experienceId={experienceId}
      title={experience.data?.title}
      initial={experience.data ? fromExperience(experience.data) : EMPTY_EXPERIENCE}
    />
  );
}

const numberValue = (v: unknown) => (v === "" || v === null || v === undefined ? Number.NaN : Number(v));

function WizardForm({ experienceId, title, initial }: { experienceId?: string; title?: string; initial: ExperienceFormValues }) {
  const t = useTranslations("experiences.hosting.wizard");
  const tErrors = useTranslations("experiences.hosting.wizard.errors");
  const tExp = useTranslations("experiences");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const schema = useMemo(() => createExperienceSchema(tErrors), [tErrors]);
  const form = useForm<ExperienceFormValues>({ resolver: zodResolver(schema), defaultValues: initial, mode: "onTouched" });
  const [step, setStep] = useState(0);
  const [visited, setVisited] = useState(experienceId ? EXPERIENCE_STEPS.length - 1 : 0);
  const save = useSaveExperience(experienceId);
  const values = useWatch({ control: form.control }) as ExperienceFormValues;
  const stepKey: ExperienceStep = EXPERIENCE_STEPS[step] ?? "basics";
  const errors = form.formState.errors;

  const goTo = useCallback((index: number) => {
    setStep(index);
    setVisited((v) => Math.max(v, index));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, []);

  const onNext = async () => {
    const ok = await form.trigger(EXPERIENCE_STEP_FIELDS[stepKey], { shouldFocus: true });
    if (ok) goTo(step + 1);
  };

  const firstInvalid = (errs: FieldErrors<ExperienceFormValues>) =>
    EXPERIENCE_STEPS.findIndex((s) => EXPERIENCE_STEP_FIELDS[s].some((f) => Boolean(errs[f])));

  const onSubmit = form.handleSubmit(
    (data) =>
      save.mutate(toExperienceRequest(data), {
        onSuccess: (id) => {
          toast.success(experienceId ? t("updated") : t("created"), { description: data.title });
          router.push(experienceId ? "/hosting/experiences" : `/hosting/experiences/${id}/sessions`);
        },
        onError: (error) => {
          const code = Object.keys(EXPERIENCE_SERVER_CODES).find((c) => hasErrorCode(error, c));
          if (code) {
            const target = EXPERIENCE_SERVER_CODES[code]!;
            form.setError(target.field, { type: "server", message: errorMessage(error) });
            goTo(EXPERIENCE_STEPS.indexOf(target.step));
          }
          toast.error(t("saveError"), { description: errorMessage(error) });
        },
      }),
    (errs) => {
      const index = firstInvalid(errs);
      if (index >= 0) {
        goTo(index);
        toast.error(t("fixStep", { step: t(`steps.${EXPERIENCE_STEPS[index]!}.title`) }));
      }
    },
  );

  const isLast = step === EXPERIENCE_STEPS.length - 1;

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-8">
      <header className="grid gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="grid gap-1">
            <p className="text-sm text-muted-foreground">
              <Link href="/hosting/experiences" className="underline-offset-4 hover:underline">
                {t("breadcrumb")}
              </Link>
            </p>
            <h1 className="text-2xl font-semibold sm:text-3xl">
              {experienceId ? t("editTitle", { title: title ?? "" }) : t("newTitle")}
            </h1>
          </div>
          <p className="text-sm font-medium text-muted-foreground" aria-live="polite">
            {t("stepOf", { step: String(step + 1), total: String(EXPERIENCE_STEPS.length) })}
          </p>
        </div>
        <div className="h-1.5 overflow-hidden rounded-full bg-muted" aria-hidden="true">
          <div
            className="h-full rounded-full bg-primary transition-[width] duration-300"
            style={{ width: `${((step + 1) / EXPERIENCE_STEPS.length) * 100}%` }}
          />
        </div>
        <ol className="-mx-1 flex gap-1 overflow-x-auto px-1 pb-1" aria-label={t("stepsLabel")}>
          {EXPERIENCE_STEPS.map((s, index) => (
            <li key={s}>
              <button
                type="button"
                onClick={() => index <= visited && goTo(index)}
                disabled={index > visited}
                aria-current={index === step ? "step" : undefined}
                className={cn(
                  "inline-flex h-8 shrink-0 items-center gap-1.5 rounded-full px-3 text-xs font-medium whitespace-nowrap transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                  index === step ? "bg-foreground text-background" : index <= visited ? "bg-muted hover:bg-muted/70" : "text-muted-foreground/60",
                )}
              >
                {index < step ? <Check className="size-3.5" aria-hidden="true" /> : <span>{index + 1}.</span>}
                {t(`steps.${s}.title`)}
              </button>
            </li>
          ))}
        </ol>
      </header>

      <section aria-labelledby="step-heading" className="grid gap-6 rounded-3xl border bg-card p-5 shadow-card sm:p-8">
        <div className="grid gap-1">
          <h2 id="step-heading" className="text-xl font-semibold">
            {t(`steps.${stepKey}.title`)}
          </h2>
          <p className="text-sm text-muted-foreground">{t(`steps.${stepKey}.description`)}</p>
        </div>

        {stepKey === "basics" && (
          <div className="grid gap-6">
            <Field id="exp-title" label={t("fields.title")} error={errors.title?.message}>
              {(control) => (
                <Input {...control} {...form.register("title")} maxLength={EXPERIENCE_LIMITS.title} placeholder={t("fields.titlePlaceholder")} data-testid="exp-title" />
              )}
            </Field>
            <Field
              id="exp-description"
              label={t("fields.description")}
              error={errors.description?.message}
              description={t("charactersLeft", { count: EXPERIENCE_LIMITS.description - (values.description?.length ?? 0) })}
            >
              {(control) => (
                <Textarea
                  {...control}
                  {...form.register("description")}
                  rows={6}
                  maxLength={EXPERIENCE_LIMITS.description}
                  placeholder={t("fields.descriptionPlaceholder")}
                  data-testid="exp-description"
                />
              )}
            </Field>
            <fieldset className="grid gap-3">
              <legend className="mb-3 text-sm font-medium">{t("fields.category")}</legend>
              <Controller
                control={form.control}
                name="category"
                render={({ field }) => (
                  <div role="radiogroup" aria-label={t("fields.category")} className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                    {EXPERIENCE_CATEGORIES.map((category) => {
                      const Icon = CATEGORY_THEME[category].icon;
                      const active = field.value === category;
                      return (
                        <button
                          key={category}
                          type="button"
                          role="radio"
                          aria-checked={active}
                          onClick={() => field.onChange(category)}
                          className={cn(
                            "flex items-center gap-3 rounded-xl border p-4 text-left text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                            active ? "border-foreground bg-muted/70 ring-1 ring-foreground" : "hover:bg-muted/50",
                          )}
                        >
                          <Icon className="size-5 shrink-0" aria-hidden="true" />
                          {tExp(`categories.${category}`)}
                        </button>
                      );
                    })}
                  </div>
                )}
              />
              {errors.category && <p className="text-xs font-medium text-destructive">{errors.category.message}</p>}
            </fieldset>
          </div>
        )}

        {stepKey === "location" && <LocationStep form={form} />}

        {stepKey === "details" && <DetailsStep form={form} />}

        {stepKey === "photos" && (
          <Controller
            control={form.control}
            name="photos"
            render={({ field, fieldState }) => (
              <div className="grid gap-3">
                <p className="text-sm text-muted-foreground">{t("photosOptional")}</p>
                <PhotosStep photos={field.value} onChange={field.onChange} error={fieldState.error?.message} />
              </div>
            )}
          />
        )}

        {stepKey === "price" && (
          <div className="grid gap-6 sm:grid-cols-[minmax(0,1fr)_10rem]">
            <Field id="exp-price" label={t("fields.price")} error={errors.pricePerGuest?.message} description={t("fields.priceHint")}>
              {(control) => (
                <Input
                  {...control}
                  type="number"
                  inputMode="decimal"
                  min={1}
                  step="0.01"
                  {...form.register("pricePerGuest", { setValueAs: numberValue })}
                  data-testid="exp-price"
                />
              )}
            </Field>
            <Field id="exp-currency" label={t("fields.currency")} error={errors.currency?.message}>
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
        )}

        {stepKey === "review" && <ReviewStep values={values} form={form} isEdit={Boolean(experienceId)} onEdit={(s) => goTo(EXPERIENCE_STEPS.indexOf(s))} />}
      </section>

      <div className="sticky bottom-0 z-20 -mx-4 flex items-center justify-between gap-3 border-t bg-background/95 px-4 py-4 backdrop-blur sm:static sm:mx-0 sm:border-0 sm:bg-transparent sm:px-0 sm:py-0">
        <Button type="button" variant="ghost" onClick={() => goTo(step - 1)} disabled={step === 0}>
          <ArrowLeft aria-hidden="true" /> {t("back")}
        </Button>
        {isLast ? (
          <Button key="submit" type="submit" size="lg" className="rounded-full" disabled={save.isPending || save.isSuccess} data-testid="exp-submit">
            {save.isPending || save.isSuccess ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Check aria-hidden="true" />}
            {experienceId ? t("saveChanges") : values.isListed ? t("publish") : t("saveUnlisted")}
          </Button>
        ) : (
          <Button key="next" type="button" size="lg" className="rounded-full" onClick={onNext} data-testid="exp-next">
            {t("next")} <ArrowRight aria-hidden="true" />
          </Button>
        )}
      </div>
    </form>
  );
}

function LocationStep({ form }: { form: FormApi }) {
  const t = useTranslations("experiences.hosting.wizard");
  const errors = form.formState.errors;
  const [lat, lng, city, country] = useWatch({ control: form.control, name: ["latitude", "longitude", "city", "country"] });
  const hasPin = Number.isFinite(lat) && Number.isFinite(lng);
  const [focus, setFocus] = useState<PinMapProps["focus"]>(null);
  const [locating, setLocating] = useState(false);
  const [notFound, setNotFound] = useState(false);

  const onPin = useCallback(
    ({ lat: nextLat, lng: nextLng }: { lat: number; lng: number }) => {
      form.setValue("latitude", nextLat, { shouldDirty: true, shouldValidate: true });
      form.setValue("longitude", nextLng, { shouldDirty: true, shouldValidate: true });
    },
    [form],
  );

  const locate = async () => {
    const query = (city || country).trim();
    if (query.length < 2) return;
    setLocating(true);
    setNotFound(false);
    try {
      const results = await apartmentsApi.suggestLocations(getBrowserHttp(), query);
      const match =
        results.find((r) => r.city.toLowerCase() === city.trim().toLowerCase()) ??
        results.find((r) => r.country.toLowerCase() === country.trim().toLowerCase()) ??
        results[0];
      if (match) setFocus({ lat: match.latitude, lng: match.longitude, zoom: 13 });
      else setNotFound(true);
    } catch {
      setNotFound(true);
    } finally {
      setLocating(false);
    }
  };

  const pinError = errors.latitude?.message ?? errors.longitude?.message;

  return (
    <div className="grid gap-6">
      <div className="grid gap-6 sm:grid-cols-2">
        <Field id="exp-city" label={t("fields.city")} error={errors.city?.message}>
          {(control) => <Input {...control} autoComplete="address-level2" maxLength={EXPERIENCE_LIMITS.location} {...form.register("city")} data-testid="exp-city" />}
        </Field>
        <Field id="exp-country" label={t("fields.country")} error={errors.country?.message}>
          {(control) => <Input {...control} autoComplete="country-name" maxLength={EXPERIENCE_LIMITS.location} {...form.register("country")} data-testid="exp-country" />}
        </Field>
      </div>
      <Field id="exp-meeting" label={t("fields.meetingPoint")} error={errors.meetingPoint?.message} description={t("fields.meetingPointHint")}>
        {(control) => (
          <Textarea
            {...control}
            rows={2}
            maxLength={EXPERIENCE_LIMITS.meetingPoint}
            placeholder={t("fields.meetingPointPlaceholder")}
            {...form.register("meetingPoint")}
            data-testid="exp-meeting"
          />
        )}
      </Field>
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
        <div className={cn("relative isolate h-80 overflow-hidden rounded-2xl border bg-muted", pinError && "border-destructive")} data-testid="pin-map">
          <PinMap value={hasPin ? { lat, lng } : null} onChange={onPin} focus={focus} />
        </div>
        <p className={cn("flex items-center gap-1.5 text-sm", pinError ? "font-medium text-destructive" : "text-muted-foreground")} aria-live="polite">
          <MapPin className="size-4" aria-hidden="true" />
          {hasPin
            ? t("location.pinned", { lat: lat.toFixed(5), lng: lng.toFixed(5) })
            : (pinError ?? (notFound ? t("location.notFound") : t("location.noPin")))}
        </p>
      </div>
    </div>
  );
}

function DetailsStep({ form }: { form: FormApi }) {
  const t = useTranslations("experiences.hosting.wizard");
  const duration = useFormatDuration();
  const languageName = useLanguageName();
  const errors = form.formState.errors;
  const [item, setItem] = useState("");

  return (
    <div className="grid gap-8">
      <div className="divide-y">
        <Controller
          control={form.control}
          name="durationMinutes"
          render={({ field }) => (
            <NumberStepper
              id="exp-duration"
              label={t("fields.duration")}
              description={duration(field.value)}
              value={field.value}
              min={EXPERIENCE_LIMITS.duration[0]}
              max={EXPERIENCE_LIMITS.duration[1]}
              step={15}
              onChange={field.onChange}
            />
          )}
        />
        <Controller
          control={form.control}
          name="maxGuests"
          render={({ field }) => (
            <NumberStepper
              id="exp-guests"
              label={t("fields.maxGuests")}
              value={field.value}
              min={EXPERIENCE_LIMITS.guests[0]}
              max={EXPERIENCE_LIMITS.guests[1]}
              onChange={field.onChange}
            />
          )}
        />
      </div>

      <Controller
        control={form.control}
        name="languages"
        render={({ field, fieldState }) => (
          <fieldset className="grid gap-3">
            <legend className="mb-1 font-semibold">{t("fields.languages")}</legend>
            <p className="text-sm text-muted-foreground">{t("fields.languagesHint", { max: EXPERIENCE_LIMITS.languages })}</p>
            <div className="flex flex-wrap gap-2">
              {LANGUAGE_CODES.map((code) => {
                const selected = field.value.includes(code);
                const full = !selected && field.value.length >= EXPERIENCE_LIMITS.languages;
                return (
                  <button
                    key={code}
                    type="button"
                    aria-pressed={selected}
                    disabled={full}
                    onClick={() => field.onChange(selected ? field.value.filter((c) => c !== code) : [...field.value, code])}
                    className={cn(
                      "inline-flex h-9 items-center gap-1.5 rounded-full border px-3.5 text-sm transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none disabled:opacity-40",
                      selected ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
                    )}
                  >
                    {selected && <Check className="size-3.5" aria-hidden="true" />}
                    {languageName(code)}
                  </button>
                );
              })}
            </div>
            {fieldState.error && <p className="text-xs font-medium text-destructive">{fieldState.error.message}</p>}
          </fieldset>
        )}
      />

      <Controller
        control={form.control}
        name="included"
        render={({ field }) => {
          const add = () => {
            const value = item.trim();
            if (!value || field.value.length >= EXPERIENCE_LIMITS.included || field.value.includes(value)) return;
            field.onChange([...field.value, value.slice(0, EXPERIENCE_LIMITS.includedItem)]);
            setItem("");
          };
          return (
            <fieldset className="grid gap-3">
              <legend className="mb-1 font-semibold">{t("fields.included")}</legend>
              <p className="text-sm text-muted-foreground">{t("fields.includedHint")}</p>
              <div className="flex gap-2">
                <label htmlFor="exp-included" className="sr-only">
                  {t("fields.includedAdd")}
                </label>
                <Input
                  id="exp-included"
                  value={item}
                  maxLength={EXPERIENCE_LIMITS.includedItem}
                  placeholder={t("fields.includedPlaceholder")}
                  onChange={(e) => setItem(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      add();
                    }
                  }}
                />
                <Button type="button" variant="outline" onClick={add} disabled={!item.trim() || field.value.length >= EXPERIENCE_LIMITS.included}>
                  <Plus aria-hidden="true" /> {t("fields.includedAdd")}
                </Button>
              </div>
              {field.value.length > 0 && (
                <ul className="flex flex-wrap gap-2">
                  {field.value.map((entry) => (
                    <li key={entry} className="inline-flex items-center gap-1 rounded-full bg-muted py-1 pr-1 pl-3 text-sm">
                      {entry}
                      <button
                        type="button"
                        onClick={() => field.onChange(field.value.filter((v) => v !== entry))}
                        className="grid size-6 place-items-center rounded-full hover:bg-background"
                        aria-label={t("fields.includedRemove", { item: entry })}
                      >
                        <X className="size-3.5" aria-hidden="true" />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              {errors.included && <p className="text-xs font-medium text-destructive">{errors.included.message}</p>}
            </fieldset>
          );
        }}
      />
    </div>
  );
}

function ReviewStep({
  values,
  form,
  isEdit,
  onEdit,
}: {
  values: ExperienceFormValues;
  form: FormApi;
  isEdit: boolean;
  onEdit: (step: ExperienceStep) => void;
}) {
  const t = useTranslations("experiences.hosting.wizard");
  const tExp = useTranslations("experiences");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const duration = useFormatDuration();
  const languageName = useLanguageName();

  const sections: { step: ExperienceStep; rows: [string, string][] }[] = [
    {
      step: "basics",
      rows: [
        [t("fields.title"), values.title],
        [t("fields.category"), tExp(`categories.${values.category}`)],
        [t("fields.description"), values.description],
      ],
    },
    {
      step: "location",
      rows: [
        [t("review.where"), [values.city, values.country].filter(Boolean).join(", ")],
        [t("fields.meetingPoint"), values.meetingPoint],
      ],
    },
    {
      step: "details",
      rows: [
        [t("fields.duration"), duration(values.durationMinutes)],
        [t("fields.maxGuests"), tCommon("guests", { count: values.maxGuests })],
        [t("fields.languages"), values.languages.map(languageName).join(", ")],
        [t("fields.included"), values.included.join(", ") || "—"],
      ],
    },
    {
      step: "price",
      rows: [[t("fields.price"), Number.isFinite(values.pricePerGuest) ? formatPrice(values.pricePerGuest, values.currency, locale) : "—"]],
    },
  ];

  return (
    <div className="grid gap-8">
      <div className="grid gap-4 sm:grid-cols-[14rem_minmax(0,1fr)] sm:items-start">
        <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted">
          <ExperienceImage src={values.photos[0]?.url} category={values.category} sizes="224px" iconClassName="size-10" />
        </div>
        <div className="grid gap-2">
          <p className="text-lg font-semibold">{values.title || "—"}</p>
          <p className="text-sm text-muted-foreground">{t("review.photos", { count: values.photos.length })}</p>
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
                    <dd className="line-clamp-3 break-words whitespace-pre-line">{value || "—"}</dd>
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
                id="exp-publish"
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

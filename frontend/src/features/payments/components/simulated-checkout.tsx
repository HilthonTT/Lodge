"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import {
  ArrowLeft,
  CalendarDays,
  CircleCheckBig,
  CreditCard,
  FlaskConical,
  Loader2,
  Lock,
  ReceiptText,
  TriangleAlert,
  Users,
} from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";

import { RemoteImage } from "@/components/shared/remote-image";
import { ErrorState, StatePanel } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { bookingPriceDetails, PriceBreakdown } from "@/features/apartments/components/price-breakdown";
import type { ValidationTranslator } from "@/features/auth/schemas";
import { useSession } from "@/features/auth/session-provider";
import { BookingStatusBadge } from "@/features/bookings/components/booking-status-badge";
import { useBooking } from "@/features/bookings/hooks/use-bookings";
import { useApiErrorMessage } from "@/i18n/api-errors";
import type { ApiError } from "@/lib/api/client";
import { formatDateRange, nightsBetween, parseDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { type Booking, BookingStatus } from "@/types/api";

import { useDisplayMoney } from "../currency-provider";
import { useCompleteSimulatedPayment, usePaymentsConfig } from "../hooks/use-payments";
import { ChargedInNote, Money } from "./money";

const TEST_CARD = "4242 4242 4242 4242";

/** Card fields are checked for format only: nothing is sent anywhere in Simulated mode. */
const createCardSchema = (t: ValidationTranslator) =>
  z.object({
    email: z.string().trim().min(1, t("emailRequired")).pipe(z.email(t("emailInvalid"))),
    cardNumber: z
      .string()
      .transform((v) => v.replace(/\s+/g, ""))
      .pipe(z.string().regex(/^\d{16}$/, t("cardNumberInvalid"))),
    expiry: z
      .string()
      .trim()
      .regex(/^(0[1-9]|1[0-2])\s?\/\s?\d{2}$/, t("expiryInvalid")),
    cvc: z.string().trim().regex(/^\d{3,4}$/, t("cvcInvalid")),
    name: z.string().trim().min(1, t("cardNameRequired")),
  });
type CardSchema = ReturnType<typeof createCardSchema>;

/** "4242424242424242" → "4242 4242 4242 4242" while typing. */
function formatCardNumber(value: string) {
  return value
    .replace(/\D/g, "")
    .slice(0, 16)
    .replace(/(\d{4})(?=\d)/g, "$1 ");
}

function formatExpiry(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 4);
  return digits.length > 2 ? `${digits.slice(0, 2)} / ${digits.slice(2)}` : digits;
}

export function SimulatedCheckout({ bookingId }: { bookingId: string | null }) {
  const t = useTranslations("payments.checkout");
  const errorMessage = useApiErrorMessage();
  const booking = useBooking(bookingId ?? undefined);
  const [paidBooking, setPaidBooking] = useState<Booking | null>(null);

  if (!bookingId) {
    return (
      <Shell>
        <ErrorState title={t("missingTitle")} description={t("missingDescription")} action={<BackToTrips />} />
      </Shell>
    );
  }

  if (paidBooking) return <PaymentSuccess booking={paidBooking} />;

  if (booking.isPending) {
    return (
      <Shell>
        <div className="grid gap-6 lg:grid-cols-2" role="status" aria-label={t("loading")}>
          <Skeleton className="h-96 rounded-2xl" />
          <Skeleton className="h-96 rounded-2xl" />
        </div>
      </Shell>
    );
  }

  if (booking.isError) {
    return (
      <Shell>
        <ErrorState title={t("loadError")} description={errorMessage(booking.error)} action={<BackToTrips />} />
      </Shell>
    );
  }

  return (
    <Shell>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:gap-10">
        <OrderSummary booking={booking.data} />
        {booking.data.status === BookingStatus.Reserved ? (
          <BookingCardForm booking={booking.data} onPaid={setPaidBooking} />
        ) : (
          <NotPayable booking={booking.data} />
        )}
      </div>
    </Shell>
  );
}

export function Shell({ children }: { children: React.ReactNode }) {
  const t = useTranslations("payments");
  return (
    <div className="mx-auto grid w-full max-w-5xl gap-6">
      <div
        role="note"
        className="flex flex-wrap items-center gap-3 rounded-2xl border-2 border-dashed border-warning/60 bg-warning/10 px-4 py-3"
        data-testid="test-mode-banner"
      >
        <span className="inline-flex items-center gap-1.5 rounded-full bg-warning px-3 py-1 text-xs font-bold tracking-wide text-black uppercase">
          <FlaskConical className="size-3.5" aria-hidden="true" /> {t("testMode")}
        </span>
        <p className="text-sm">{t("testModeText")}</p>
      </div>
      {children}
    </div>
  );
}

export function BackToTrips() {
  const t = useTranslations("payments.checkout");
  return (
    <Button asChild variant="outline">
      <Link href="/bookings">{t("backToTrips")}</Link>
    </Button>
  );
}

function OrderSummary({ booking }: { booking: Booking }) {
  const t = useTranslations("payments.checkout");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const converted = useDisplayMoney().convert(booking.totalPriceAmount, booking.totalPriceCurrency).converted;

  return (
    <section aria-labelledby="summary-heading" className="grid content-start gap-5">
      <Link
        href={`/bookings?highlight=${encodeURIComponent(booking.id)}`}
        className="inline-flex items-center gap-1.5 justify-self-start text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden="true" /> {t("backToTrips")}
      </Link>
      <div className="grid gap-1">
        <p className="text-sm text-muted-foreground">{t("payLodge")}</p>
        <p className="text-4xl font-semibold tracking-tight tabular-nums" data-testid="checkout-total">
          {formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale)}
        </p>
        {converted && (
          <Money
            amount={booking.totalPriceAmount}
            currency={booking.totalPriceCurrency}
            tooltip
            className="justify-self-start text-sm text-muted-foreground"
          />
        )}
      </div>
      <div className="grid gap-5 rounded-2xl border bg-card p-5 shadow-card">
        <div className="flex gap-4">
          <div className="relative size-20 shrink-0 overflow-hidden rounded-xl bg-muted">
            <RemoteImage src={booking.apartmentImageUrl} alt="" fill sizes="80px" className="object-cover" />
          </div>
          <div className="grid min-w-0 content-center gap-1 text-sm">
            <h1 id="summary-heading" className="line-clamp-2 text-base font-semibold">
              {booking.apartmentName}
            </h1>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
              {start && end ? formatDateRange(start, end, locale) : "—"}
              {nights > 0 && ` · ${tCommon("nights", { count: nights })}`}
            </p>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <Users className="size-4 shrink-0" aria-hidden="true" />
              {tCommon("guests", { count: booking.guestCount || 1 })}
            </p>
          </div>
        </div>
        <Separator />
        <PriceBreakdown price={bookingPriceDetails(booking)} totalLabel={t("totalDue")} />
        <ChargedInNote currency={booking.totalPriceCurrency} />
      </div>
    </section>
  );
}

function BookingCardForm({ booking, onPaid }: { booking: Booking; onPaid: (booking: Booking) => void }) {
  const complete = useCompleteSimulatedPayment();
  return (
    <CardForm
      amount={booking.totalPriceAmount}
      currency={booking.totalPriceCurrency}
      pending={complete.isPending}
      error={complete.error}
      onPay={async () => {
        await complete.mutateAsync(booking.id);
        onPaid({ ...booking, status: BookingStatus.Confirmed });
      }}
    />
  );
}

/** Test-mode card form; `onPay` completes the payment (errors are shown via `error`). */
export function CardForm({
  amount: total,
  currency,
  pending,
  error,
  onPay,
}: {
  amount: number;
  currency: string;
  pending: boolean;
  error: ApiError | null;
  onPay: () => Promise<void>;
}) {
  const t = useTranslations("payments.checkout");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const { session } = useSession();
  const config = usePaymentsConfig();
  const simulated = config.data?.mode === "Simulated";

  const schema = useMemo(() => createCardSchema(tValidation), [tValidation]);
  const form = useForm<z.input<CardSchema>, unknown, z.output<CardSchema>>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: session?.email ?? "",
      cardNumber: TEST_CARD,
      expiry: "12 / 34",
      cvc: "123",
      name: session?.name ?? "",
    },
  });
  const { errors } = form.formState;
  const amount = formatPrice(total, currency, locale);

  const onSubmit = form.handleSubmit(async () => {
    try {
      await onPay();
    } catch {
      // shown inline
    }
  });

  return (
    <section aria-labelledby="pay-heading" className="grid content-start gap-5 rounded-2xl border bg-card p-6 shadow-float">
      <div className="flex items-center justify-between gap-3">
        <h2 id="pay-heading" className="text-lg font-semibold">
          {t("payWithCard")}
        </h2>
        <CreditCard className="size-5 text-muted-foreground" aria-hidden="true" />
      </div>

      {config.data && !simulated && (
        <p role="alert" className="flex items-start gap-2 rounded-xl bg-destructive/10 p-3 text-sm text-destructive">
          <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          {t("notSimulated")}
        </p>
      )}

      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        <Field id="pay-email" label={t("email")} error={errors.email?.message}>
          {(control) => <Input {...control} type="email" autoComplete="email" {...form.register("email")} />}
        </Field>
        <fieldset className="grid gap-2">
          <legend className="mb-2 text-sm font-medium">{t("cardInformation")}</legend>
          <div className="grid overflow-hidden rounded-xl border">
            <label className="sr-only" htmlFor="pay-card">
              {t("cardNumber")}
            </label>
            <Input
              id="pay-card"
              inputMode="numeric"
              autoComplete="cc-number"
              aria-invalid={Boolean(errors.cardNumber)}
              className="rounded-none border-0 border-b font-mono shadow-none focus-visible:ring-inset"
              {...form.register("cardNumber", {
                onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
                  form.setValue("cardNumber", formatCardNumber(e.target.value)),
              })}
            />
            <div className="grid grid-cols-2 divide-x">
              <label className="sr-only" htmlFor="pay-expiry">
                {t("expiry")}
              </label>
              <Input
                id="pay-expiry"
                inputMode="numeric"
                autoComplete="cc-exp"
                placeholder={t("expiryPlaceholder")}
                aria-invalid={Boolean(errors.expiry)}
                className="rounded-none border-0 font-mono shadow-none focus-visible:ring-inset"
                {...form.register("expiry", {
                  onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
                    form.setValue("expiry", formatExpiry(e.target.value)),
                })}
              />
              <label className="sr-only" htmlFor="pay-cvc">
                {t("cvc")}
              </label>
              <Input
                id="pay-cvc"
                inputMode="numeric"
                autoComplete="cc-csc"
                placeholder={t("cvc")}
                maxLength={4}
                aria-invalid={Boolean(errors.cvc)}
                className="rounded-none border-0 font-mono shadow-none focus-visible:ring-inset"
                {...form.register("cvc")}
              />
            </div>
          </div>
          {[errors.cardNumber, errors.expiry, errors.cvc].map(
            (error, index) =>
              error?.message && (
                <p key={index} className="text-xs font-medium text-destructive">
                  {error.message}
                </p>
              ),
          )}
          <p className="text-xs text-muted-foreground">{t("testCardHint", { card: TEST_CARD })}</p>
        </fieldset>
        <Field id="pay-name" label={t("nameOnCard")} error={errors.name?.message}>
          {(control) => <Input {...control} autoComplete="cc-name" {...form.register("name")} />}
        </Field>

        {error && (
          <p role="alert" className="flex items-start gap-2 rounded-xl bg-destructive/10 p-3 text-sm text-destructive">
            <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            {errorMessage(error)}
          </p>
        )}

        <Button type="submit" size="lg" className="w-full" disabled={!simulated || pending}>
          {pending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Lock aria-hidden="true" />}
          {pending ? t("processing") : t("pay", { amount })}
        </Button>
        <p className="text-center text-xs text-muted-foreground">{t("noRealCharge")}</p>
      </form>
    </section>
  );
}

function NotPayable({ booking }: { booking: Booking }) {
  const t = useTranslations("payments.checkout");
  return (
    <StatePanel
      icon={ReceiptText}
      title={booking.status === BookingStatus.Confirmed ? t("alreadyPaidTitle") : t("notPayableTitle")}
      description={
        <span className="grid justify-items-center gap-2">
          <BookingStatusBadge status={booking.status} />
          {booking.status === BookingStatus.Confirmed ? t("alreadyPaidText") : t("notPayableText")}
        </span>
      }
      action={
        <Button asChild>
          <Link href={`/bookings?highlight=${encodeURIComponent(booking.id)}`}>{t("viewTrip")}</Link>
        </Button>
      }
      className="bg-card"
    />
  );
}

function PaymentSuccess({ booking }: { booking: Booking }) {
  const t = useTranslations("payments.checkout");
  const locale = useLocale();
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  return (
    <Shell>
      <section
        role="status"
        aria-labelledby="success-heading"
        className="mx-auto grid w-full max-w-lg justify-items-center gap-5 rounded-3xl border bg-card p-8 text-center shadow-float"
        data-testid="payment-success"
      >
        <span className="grid size-16 place-items-center rounded-full bg-success/15 text-success">
          <CircleCheckBig className="size-8" aria-hidden="true" />
        </span>
        <div className="grid gap-2">
          <h1 id="success-heading" className="text-2xl font-semibold">
            {t("successTitle")}
          </h1>
          <p className="text-muted-foreground">
            {t("successText", {
              amount: formatPrice(booking.totalPriceAmount, booking.totalPriceCurrency, locale),
              name: booking.apartmentName,
              dates: start && end ? formatDateRange(start, end, locale) : "",
            })}
          </p>
        </div>
        <BookingStatusBadge status={BookingStatus.Confirmed} />
        <div className="flex flex-wrap justify-center gap-2">
          <Button asChild size="lg">
            <Link href={`/bookings?highlight=${encodeURIComponent(booking.id)}`}>{t("viewTrip")}</Link>
          </Button>
          <Button asChild size="lg" variant="outline">
            <Link href={`/apartments/${booking.apartmentId}`}>{t("viewListing")}</Link>
          </Button>
        </div>
      </section>
    </Shell>
  );
}

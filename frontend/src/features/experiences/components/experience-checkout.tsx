"use client";

import { ArrowLeft, CalendarClock, CircleCheckBig, MapPin, ReceiptText, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { ErrorState, StatePanel } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { ChargedInNote, Money } from "@/features/payments/components/money";
import { BackToTrips, CardForm, Shell } from "@/features/payments/components/simulated-checkout";
import { useDisplayMoney } from "@/features/payments/currency-provider";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatSessionDateTime } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import type { ExperienceBooking } from "@/types/api";

import { ExperienceImage, ExperienceStatusBadge } from "../experience-options";
import { useCompleteSimulatedExperiencePayment, useExperienceBooking } from "../hooks/use-experiences";

const tripHref = (id: string) => `/experience-bookings/${encodeURIComponent(id)}`;

/** Simulated hosted payment page for an experience booking (`?experienceBookingId=`). */
export function ExperienceSimulatedCheckout({ bookingId }: { bookingId: string }) {
  const t = useTranslations("payments.checkout");
  const errorMessage = useApiErrorMessage();
  const booking = useExperienceBooking(bookingId);
  const complete = useCompleteSimulatedExperiencePayment();
  const [paid, setPaid] = useState<ExperienceBooking | null>(null);

  if (paid) return <ExperiencePaymentSuccess booking={paid} />;

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

  const b = booking.data;
  return (
    <Shell>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:gap-10">
        <ExperienceOrderSummary booking={b} />
        {b.status === "Reserved" ? (
          <CardForm
            amount={b.totalPrice}
            currency={b.currency}
            pending={complete.isPending}
            error={complete.error}
            onPay={async () => {
              await complete.mutateAsync(b.id);
              setPaid({ ...b, status: "Confirmed" });
            }}
          />
        ) : (
          <StatePanel
            icon={ReceiptText}
            title={b.status === "Confirmed" ? t("alreadyPaidTitle") : t("notPayableTitle")}
            description={
              <span className="grid justify-items-center gap-2">
                <ExperienceStatusBadge status={b.status} />
                {b.status === "Confirmed" ? t("alreadyPaidText") : t("notPayableText")}
              </span>
            }
            action={
              <Button asChild>
                <Link href={tripHref(b.id)}>{t("viewTrip")}</Link>
              </Button>
            }
            className="bg-card"
          />
        )}
      </div>
    </Shell>
  );
}

function ExperienceOrderSummary({ booking: b }: { booking: ExperienceBooking }) {
  const t = useTranslations("payments.checkout");
  const tExp = useTranslations("experiences.trips");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const converted = useDisplayMoney().convert(b.totalPrice, b.currency).converted;

  return (
    <section aria-labelledby="summary-heading" className="grid content-start gap-5">
      <Link
        href="/bookings?tab=experiences"
        className="inline-flex items-center gap-1.5 justify-self-start text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden="true" /> {t("backToTrips")}
      </Link>
      <div className="grid gap-1">
        <p className="text-sm text-muted-foreground">{t("payLodge")}</p>
        <p className="text-4xl font-semibold tracking-tight tabular-nums" data-testid="checkout-total">
          {formatPrice(b.totalPrice, b.currency, locale)}
        </p>
        {converted && (
          <Money amount={b.totalPrice} currency={b.currency} tooltip className="justify-self-start text-sm text-muted-foreground" />
        )}
      </div>
      <div className="grid gap-5 rounded-2xl border bg-card p-5 shadow-card">
        <div className="flex gap-4">
          <div className="relative size-20 shrink-0 overflow-hidden rounded-xl bg-muted">
            <ExperienceImage src={b.imageUrl} category={b.category} sizes="80px" iconClassName="size-8" />
          </div>
          <div className="grid min-w-0 content-center gap-1 text-sm">
            <h1 id="summary-heading" className="line-clamp-2 text-base font-semibold">
              {b.experienceTitle}
            </h1>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <CalendarClock className="size-4 shrink-0" aria-hidden="true" />
              {formatSessionDateTime(new Date(b.startsAtUtc), locale)}
            </p>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <Users className="size-4 shrink-0" aria-hidden="true" />
              {tCommon("guests", { count: b.guestCount })}
            </p>
            <p className="flex items-center gap-1.5 text-muted-foreground">
              <MapPin className="size-4 shrink-0" aria-hidden="true" />
              {[b.city, b.country].filter(Boolean).join(", ")}
            </p>
          </div>
        </div>
        <Separator />
        <ExperiencePriceRows booking={b} />
        <ChargedInNote currency={b.currency} />
        <p className="text-xs text-muted-foreground">
          {tExp("freeUntil", { date: formatSessionDateTime(new Date(b.freeCancellationUntilUtc), locale) })}
        </p>
      </div>
    </section>
  );
}

/** Price, service fee and total of an experience booking. */
export function ExperiencePriceRows({ booking: b }: { booking: ExperienceBooking }) {
  const t = useTranslations("experiences.trips");
  const locale = useLocale();
  return (
    <dl className="grid gap-2 text-sm">
      <div className="flex justify-between gap-3">
        <dt className="text-muted-foreground">{t("priceForGuests", { count: b.guestCount })}</dt>
        <dd className="tabular-nums">{formatPrice(b.priceTotal, b.currency, locale)}</dd>
      </div>
      <div className="flex justify-between gap-3">
        <dt className="text-muted-foreground">{t("serviceFee")}</dt>
        <dd className="tabular-nums">{formatPrice(b.serviceFee, b.currency, locale)}</dd>
      </div>
      <div className="flex justify-between gap-3 border-t pt-2 font-semibold">
        <dt>{t("total")}</dt>
        <dd className="tabular-nums">{formatPrice(b.totalPrice, b.currency, locale)}</dd>
      </div>
    </dl>
  );
}

function ExperiencePaymentSuccess({ booking: b }: { booking: ExperienceBooking }) {
  const t = useTranslations("payments.checkout");
  const tExp = useTranslations("experiences.checkout");
  const locale = useLocale();
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
            {tExp("successText", {
              amount: formatPrice(b.totalPrice, b.currency, locale),
              title: b.experienceTitle,
              date: formatSessionDateTime(new Date(b.startsAtUtc), locale),
            })}
          </p>
        </div>
        <ExperienceStatusBadge status="Confirmed" />
        <div className="flex flex-wrap justify-center gap-2">
          <Button asChild size="lg">
            <Link href={tripHref(b.id)}>{t("viewTrip")}</Link>
          </Button>
          <Button asChild size="lg" variant="outline">
            <Link href={`/experiences/${b.experienceId}`}>{tExp("viewExperience")}</Link>
          </Button>
        </div>
      </section>
    </Shell>
  );
}

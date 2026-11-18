"use client";

import { CalendarClock, CircleCheck, Clock, ListChecks, MapPin, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { useUserId } from "@/features/auth/session-provider";
import { ChargedInNote } from "@/features/payments/components/money";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatMediumDate, formatSessionDateTime } from "@/lib/dates";
import { formatPrice } from "@/lib/format";

import { ExperienceImage, ExperienceStatusBadge, useFormatDuration } from "../experience-options";
import { useExperienceBooking } from "../hooks/use-experiences";
import { ExperiencePriceRows } from "./experience-checkout";
import { ExperienceTripActions } from "./experience-trip-actions";
import { ExperienceRefund } from "./experience-trips";

/** /experience-bookings/[id]: one experience booking for its guest (or the host). */
export function ExperienceBookingDetail({ bookingId }: { bookingId: string }) {
  const t = useTranslations("experiences.trips");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const userId = useUserId();
  const duration = useFormatDuration();
  const booking = useExperienceBooking(bookingId);

  if (booking.isPending) {
    return (
      <div className="grid gap-6" role="status" aria-label={t("loading")}>
        <Skeleton className="h-64 rounded-3xl" />
        <Skeleton className="h-40 rounded-2xl" />
      </div>
    );
  }
  if (booking.isError) {
    return (
      <ErrorState
        title={booking.error.isNotFound ? t("notFound") : t("loadError")}
        description={errorMessage(booking.error)}
        action={
          <Button asChild variant="outline">
            <Link href="/bookings?tab=experiences">{t("backToTrips")}</Link>
          </Button>
        }
      />
    );
  }

  const b = booking.data;
  const isGuest = b.guestId === userId;
  const start = new Date(b.startsAtUtc);

  return (
    <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem] lg:gap-12">
      <div className="grid content-start gap-6">
        <div className="relative h-56 overflow-hidden rounded-3xl bg-muted sm:h-72">
          <ExperienceImage src={b.imageUrl} category={b.category} sizes="(min-width: 1024px) 60vw, 100vw" priority iconClassName="size-20" />
        </div>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="grid gap-1">
            <h1 className="text-2xl font-semibold sm:text-3xl">{b.experienceTitle}</h1>
            <p className="text-muted-foreground">{t("hostedBy", { name: b.hostFirstName })}</p>
          </div>
          <ExperienceStatusBadge status={b.status} />
        </div>
        <dl className="grid gap-4 sm:grid-cols-2">
          {[
            { icon: CalendarClock, label: t("when"), value: formatSessionDateTime(start, locale) },
            { icon: Clock, label: t("duration"), value: duration(b.durationMinutes) },
            { icon: Users, label: t("guests"), value: tCommon("guests", { count: b.guestCount }) },
            { icon: MapPin, label: t("meetingPoint"), value: b.meetingPoint || [b.city, b.country].filter(Boolean).join(", ") },
          ].map((row) => (
            <div key={row.label} className="flex items-start gap-3 rounded-2xl border p-4">
              <row.icon className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
              <div className="grid gap-0.5">
                <dt className="text-sm text-muted-foreground">{row.label}</dt>
                <dd className="font-medium">{row.value}</dd>
              </div>
            </div>
          ))}
        </dl>
        {b.status === "Cancelled" && <ExperienceRefund booking={b} />}
        {isGuest && (
          <div className="flex flex-wrap gap-2">
            <ExperienceTripActions booking={b} />
            <Button asChild variant="ghost">
              <Link href={`/experiences/${b.experienceId}`}>{t("viewExperience")}</Link>
            </Button>
          </div>
        )}
      </div>
      <aside className="grid content-start gap-4 rounded-3xl border bg-card p-6 shadow-card lg:sticky lg:top-28 lg:self-start">
        <h2 className="font-semibold">{t("priceDetails")}</h2>
        <ExperiencePriceRows booking={b} />
        <ChargedInNote currency={b.currency} />
        {b.paidOnUtc && b.amountPaid !== null && (
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <CircleCheck className="size-3.5 text-success" aria-hidden="true" />
            {t("paidOn", { amount: formatPrice(b.amountPaid, b.currency, locale), date: formatMediumDate(new Date(b.paidOnUtc), locale) })}
          </p>
        )}
        <Separator />
        <p className="text-sm text-muted-foreground">
          {b.status === "Cancelled" || b.status === "Completed"
            ? t("policy")
            : t("freeUntil", { date: formatSessionDateTime(new Date(b.freeCancellationUntilUtc), locale) })}
        </p>
        {isGuest ? (
          <Button asChild variant="outline">
            <Link href="/bookings?tab=experiences">{t("backToTrips")}</Link>
          </Button>
        ) : (
          <Button asChild variant="outline">
            <Link href={`/hosting/experiences/${b.experienceId}/bookings`}>
              <ListChecks aria-hidden="true" /> {t("hostBookings")}
            </Link>
          </Button>
        )}
      </aside>
    </div>
  );
}

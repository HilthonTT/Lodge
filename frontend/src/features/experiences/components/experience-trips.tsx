"use client";

import { CalendarClock, Compass, MapPin, ShieldAlert, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { RefundStatusBadge } from "@/features/bookings/components/refund-status";
import { ChargedAmount } from "@/features/payments/components/money";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatSessionDateTime } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { ExperienceBooking } from "@/types/api";

import { ExperienceImage, ExperienceStatusBadge, useFormatDuration } from "../experience-options";
import { useUserExperienceBookings } from "../hooks/use-experiences";
import { ExperienceTripActions } from "./experience-trip-actions";

/** The guest's experience bookings (trips page, Experiences tab). */
export function ExperienceTrips({ highlight }: { highlight?: string | null }) {
  const t = useTranslations("experiences.trips");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const bookings = useUserExperienceBookings();

  if (bookings.isPending) {
    return (
      <div className="grid gap-4" role="status" aria-label={t("loading")}>
        {Array.from({ length: 2 }, (_, i) => (
          <Skeleton key={i} className="h-40 rounded-2xl" />
        ))}
      </div>
    );
  }
  if (bookings.isError) {
    return (
      <ErrorState
        title={t("loadError")}
        description={errorMessage(bookings.error)}
        action={
          <Button variant="outline" onClick={() => bookings.refetch()}>
            {tCommon("tryAgain")}
          </Button>
        }
      />
    );
  }
  if (bookings.data.length === 0) {
    return (
      <EmptyState
        icon={Compass}
        title={t("emptyTitle")}
        description={t("emptyDescription")}
        action={
          <Button asChild>
            <Link href="/experiences">{t("explore")}</Link>
          </Button>
        }
      />
    );
  }

  return (
    <ul className="grid gap-4" data-testid="experience-trips">
      {bookings.data.map((booking) => (
        <li key={booking.id}>
          <ExperienceTripCard booking={booking} highlighted={booking.id === highlight} />
        </li>
      ))}
    </ul>
  );
}

export function ExperienceTripCard({ booking: b, highlighted }: { booking: ExperienceBooking; highlighted?: boolean }) {
  const t = useTranslations("experiences.trips");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const duration = useFormatDuration();
  const href = `/experience-bookings/${b.id}`;

  return (
    <article
      className={cn(
        "grid gap-4 rounded-2xl border bg-card p-4 shadow-card sm:grid-cols-[11rem_minmax(0,1fr)] sm:gap-5",
        highlighted && "ring-2 ring-primary/60",
      )}
      data-testid="experience-trip"
      data-status={b.status}
    >
      <Link href={href} tabIndex={-1} aria-hidden="true" className="relative aspect-[4/3] overflow-hidden rounded-xl bg-muted sm:aspect-auto sm:min-h-32">
        <ExperienceImage src={b.imageUrl} category={b.category} sizes="(min-width: 640px) 176px, 100vw" iconClassName="size-10" />
      </Link>
      <div className="flex min-w-0 flex-col gap-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <h3 className="min-w-0 font-semibold">
            <Link href={href} className="line-clamp-1 hover:underline">
              {b.experienceTitle}
            </Link>
          </h3>
          <ExperienceStatusBadge status={b.status} />
        </div>
        <dl className="grid gap-1.5 text-sm sm:grid-cols-2">
          <div className="flex items-center gap-2 text-muted-foreground">
            <dt className="sr-only">{t("when")}</dt>
            <CalendarClock className="size-4 shrink-0" aria-hidden="true" />
            <dd>
              {formatSessionDateTime(new Date(b.startsAtUtc), locale)} · {duration(b.durationMinutes)}
            </dd>
          </div>
          <div className="flex items-start gap-2 sm:justify-end">
            <dt className="text-muted-foreground">{t("total")}</dt>
            <dd className="font-semibold tabular-nums">
              <ChargedAmount amount={b.totalPrice} currency={b.currency} />
            </dd>
          </div>
          <div className="flex items-center gap-2 text-muted-foreground">
            <dt className="sr-only">{t("guests")}</dt>
            <Users className="size-4 shrink-0" aria-hidden="true" />
            <dd>{tCommon("guests", { count: b.guestCount })}</dd>
          </div>
          <div className="flex items-center gap-2 text-muted-foreground sm:justify-end">
            <dt className="sr-only">{t("where")}</dt>
            <MapPin className="size-4 shrink-0" aria-hidden="true" />
            <dd className="line-clamp-1">{[b.city, b.country].filter(Boolean).join(", ")}</dd>
          </div>
        </dl>
        {b.status === "Cancelled" && <ExperienceRefund booking={b} />}
        <div className="mt-auto flex flex-wrap items-center gap-2 pt-1">
          <Button asChild variant="ghost">
            <Link href={href}>{t("details")}</Link>
          </Button>
          <ExperienceTripActions booking={b} />
        </div>
      </div>
    </article>
  );
}

/** Refund amount and status of a cancelled experience booking. */
export function ExperienceRefund({ booking: b }: { booking: ExperienceBooking }) {
  const t = useTranslations("experiences.trips");
  const hasRefund = b.refundAmount !== null && b.refundAmount > 0;
  return (
    <div className={cn("grid gap-2 rounded-xl p-3 text-sm", b.cancelledByHost ? "border border-warning/40 bg-warning/10" : "bg-muted/60")}>
      {b.cancelledByHost && (
        <p className="flex items-start gap-2">
          <ShieldAlert className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true" />
          <span className="font-medium">{t("cancelledByHost")}</span>
        </p>
      )}
      {hasRefund ? (
        <div className="flex flex-wrap items-center justify-between gap-2" data-testid="experience-refund">
          <span className="flex flex-wrap items-baseline gap-1.5">
            <span className="text-muted-foreground">{t("refund")}</span>
            <ChargedAmount amount={b.refundAmount!} currency={b.currency} className="justify-items-start font-semibold tabular-nums" />
          </span>
          <RefundStatusBadge status={b.refundStatus} />
        </div>
      ) : (
        <p className="text-muted-foreground">{b.amountPaid ? t("noRefund") : t("notCharged")}</p>
      )}
    </div>
  );
}

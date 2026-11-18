"use client";

import { CalendarClock, ListChecks, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState } from "@/components/shared/state";
import { Skeleton } from "@/components/ui/skeleton";
import { RefundStatusBadge } from "@/features/bookings/components/refund-status";
import { HostingError } from "@/features/hosting/components/hosting-gate";
import { formatSessionDateTime } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";

import { ExperienceStatusBadge } from "../../experience-options";
import { useHostExperience, useHostExperienceBookings } from "../../hooks/use-experiences";

export const HOST_EXPERIENCE_BOOKINGS_PAGE_SIZE = 20;

/** Bookings for one of the host's experiences, newest first. */
export function HostExperienceBookings({ experienceId, page }: { experienceId: string; page: number }) {
  const t = useTranslations("experiences.hosting.bookings");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const experience = useHostExperience(experienceId);
  const bookings = useHostExperienceBookings({ page, pageSize: HOST_EXPERIENCE_BOOKINGS_PAGE_SIZE, experienceId });
  const totalPages = bookings.data ? Math.ceil(bookings.data.totalCount / HOST_EXPERIENCE_BOOKINGS_PAGE_SIZE) : 0;

  return (
    <div className="grid gap-8">
      <header className="grid gap-1">
        <p className="text-sm text-muted-foreground">
          <Link href="/hosting/experiences" className="underline-offset-4 hover:underline">
            {t("breadcrumb")}
          </Link>
        </p>
        <h1 className="text-2xl font-semibold sm:text-3xl">
          {experience.data ? t("title", { title: experience.data.title }) : " "}
        </h1>
        <p className="text-muted-foreground">
          <Link href={`/hosting/experiences/${experienceId}/sessions`} className="underline-offset-4 hover:underline">
            {t("manageSessions")}
          </Link>
        </p>
      </header>

      {bookings.isPending ? (
        <div className="grid gap-3" role="status" aria-label={t("loading")}>
          {Array.from({ length: 3 }, (_, i) => (
            <Skeleton key={i} className="h-24 rounded-2xl" />
          ))}
        </div>
      ) : bookings.isError ? (
        <HostingError error={bookings.error} title={t("loadError")} onRetry={() => bookings.refetch()} />
      ) : bookings.data.items.length === 0 ? (
        <EmptyState icon={ListChecks} title={t("emptyTitle")} description={t("emptyDescription")} />
      ) : (
        <>
          <ul className={cn("grid gap-3", bookings.isPlaceholderData && "opacity-60")} data-testid="host-experience-bookings">
            {bookings.data.items.map((b) => (
              <li key={b.id}>
                <article className="flex flex-wrap items-center gap-x-6 gap-y-2 rounded-2xl border bg-card p-4 shadow-card">
                  <div className="grid min-w-40 flex-1 gap-0.5">
                    <Link href={`/experience-bookings/${b.id}`} className="font-semibold hover:underline">
                      {b.guestName}
                    </Link>
                    <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
                      <CalendarClock className="size-4" aria-hidden="true" />
                      {formatSessionDateTime(new Date(b.startsAtUtc), locale)}
                    </p>
                  </div>
                  <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
                    <Users className="size-4" aria-hidden="true" />
                    {tCommon("guests", { count: b.guestCount })}
                  </p>
                  <p className="text-sm">
                    <span className="text-muted-foreground">{t("total")} </span>
                    <span className="font-semibold tabular-nums">{formatPrice(b.totalPrice, b.currency, locale)}</span>
                  </p>
                  <div className="flex flex-wrap items-center gap-2">
                    <ExperienceStatusBadge status={b.status} />
                    <RefundStatusBadge status={b.refundStatus} />
                  </div>
                </article>
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={totalPages} pathname={`/hosting/experiences/${experienceId}/bookings`} />
        </>
      )}
    </div>
  );
}

"use client";

import { Award, Banknote, CalendarCheck, Home, Inbox, type LucideIcon, Plus, Star, Wallet } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useMemo } from "react";

import { EmptyState } from "@/components/shared/state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/features/auth/session-provider";
import { parseDateOnly, today } from "@/lib/dates";
import { formatNumber, formatPrice, formatRating } from "@/lib/format";
import { cn } from "@/lib/utils";
import { BookingStatus, type HostingSummary } from "@/types/api";

import { useHostBookings } from "../hooks/use-hosting";
import { useHostingSummary } from "../hooks/use-hosting-summary";
import { HostBookingCard } from "./host-booking-card";
import { HostingError } from "./hosting-gate";

export function HostingDashboard() {
  const t = useTranslations("hosting.dashboard");
  const { session } = useSession();
  const summary = useHostingSummary();
  const firstName = session?.name.split(" ")[0] ?? "";

  return (
    <div className="grid gap-10">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="grid gap-1">
          <h1 className="text-2xl font-semibold sm:text-3xl">{t("welcome", { name: firstName })}</h1>
          <p className="text-muted-foreground">{t("intro")}</p>
        </div>
        <Button asChild className="rounded-full">
          <Link href="/hosting/listings/new">
            <Plus aria-hidden="true" /> {t("createListing")}
          </Link>
        </Button>
      </header>

      {summary.isPending ? (
        <SummarySkeleton />
      ) : summary.isError ? (
        <HostingError error={summary.error} title={t("loadError")} onRetry={() => summary.refetch()} />
      ) : (
        <>
          <SummaryCards summary={summary.data} />
          <SuperhostProgress summary={summary.data} />
        </>
      )}

      <PendingRequests />
      <UpcomingStays />
    </div>
  );
}

function SummaryCards({ summary }: { summary: HostingSummary }) {
  const t = useTranslations("hosting.dashboard");
  const locale = useLocale();
  const cards: { key: string; icon: LucideIcon; label: string; value: string; hint?: string; href?: string }[] = [
    {
      key: "pending",
      icon: Inbox,
      label: t("pendingRequests"),
      value: formatNumber(summary.pendingRequests, locale),
      href: "/hosting/reservations?status=pending",
    },
    {
      key: "upcoming",
      icon: CalendarCheck,
      label: t("upcomingStays"),
      value: formatNumber(summary.upcomingStays, locale),
      href: "/hosting/reservations",
    },
    {
      key: "earnings",
      icon: Wallet,
      label: t("earningsThisMonth"),
      value: formatPrice(summary.earningsThisMonth, summary.currency, locale),
      hint: t("paidTotal", { amount: formatPrice(summary.paidTotal, summary.currency, locale) }),
      href: "/hosting/payouts",
    },
    {
      key: "payouts",
      icon: Banknote,
      label: t("upcomingPayouts"),
      value: formatPrice(summary.upcomingPayouts, summary.currency, locale),
      hint: t("upcomingPayoutsHint"),
      href: "/hosting/payouts",
    },
    {
      key: "rating",
      icon: Star,
      label: t("rating"),
      value: summary.averageRating !== null ? formatRating(summary.averageRating, locale) : "—",
      hint: t("reviews", { count: summary.reviewCount }),
    },
    {
      key: "listings",
      icon: Home,
      label: t("listings"),
      value: formatNumber(summary.listingCount, locale),
      href: "/hosting/listings",
    },
  ];

  return (
    <ul className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-6">
      {cards.map((card) => {
        const Icon = card.icon;
        const body = (
          <>
            <span className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
              {card.label}
              <Icon className="size-4" aria-hidden="true" />
            </span>
            <span className="text-2xl font-semibold tabular-nums">{card.value}</span>
            {card.hint && <span className="text-xs text-muted-foreground">{card.hint}</span>}
          </>
        );
        const className =
          "grid h-full content-start gap-1.5 rounded-2xl border bg-card p-5 shadow-card transition-shadow";
        return (
          <li key={card.key}>
            {card.href ? (
              <Link
                href={card.href}
                className={cn(className, "hover:shadow-float focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none")}
              >
                {body}
              </Link>
            ) : (
              <div className={className}>{body}</div>
            )}
          </li>
        );
      })}
    </ul>
  );
}

function ProgressBar({ value, met, label }: { value: number; met: boolean; label: string }) {
  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(value * 100)}
      className="h-2 overflow-hidden rounded-full bg-muted"
    >
      <div
        className={cn("h-full rounded-full transition-[width]", met ? "bg-primary" : "bg-warning")}
        style={{ width: `${Math.max(4, Math.min(100, value * 100))}%` }}
      />
    </div>
  );
}

function SuperhostProgress({ summary }: { summary: HostingSummary }) {
  const t = useTranslations("hosting.superhost");
  const locale = useLocale();
  const { superhost } = summary;
  const { requirements } = superhost;
  const pct = (v: number) => `${formatNumber(Math.round(v * 1000) / 10, locale)}%`;

  const rows = [
    {
      key: "stays",
      label: t("completedStays"),
      current: formatNumber(superhost.completedStays, locale),
      target: t("atLeast", { value: formatNumber(requirements.minCompletedStays, locale) }),
      progress: requirements.minCompletedStays ? superhost.completedStays / requirements.minCompletedStays : 1,
      met: superhost.completedStays >= requirements.minCompletedStays,
    },
    {
      key: "rating",
      label: t("averageRating"),
      current: superhost.averageRating !== null ? formatRating(superhost.averageRating, locale) : "—",
      target: t("atLeast", { value: formatRating(requirements.minAverageRating, locale) }),
      progress: superhost.averageRating !== null ? superhost.averageRating / requirements.minAverageRating : 0,
      met: superhost.averageRating !== null && superhost.averageRating >= requirements.minAverageRating,
    },
    {
      key: "cancellations",
      label: t("cancellationRate"),
      current: pct(superhost.cancellationRate),
      target: t("atMost", { value: pct(requirements.maxCancellationRate) }),
      // Lower is better: full bar at 0 %, empty at twice the limit.
      progress: Math.max(0, 1 - superhost.cancellationRate / (requirements.maxCancellationRate * 2 || 1)),
      met: superhost.cancellationRate <= requirements.maxCancellationRate,
    },
  ];

  return (
    <section aria-labelledby="superhost-heading" className="grid gap-5 rounded-2xl border bg-card p-6 shadow-card">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-accent text-accent-foreground">
            <Award className="size-5" aria-hidden="true" />
          </span>
          <div className="grid gap-0.5">
            <h2 id="superhost-heading" className="text-lg font-semibold">
              {t("title")}
            </h2>
            <p className="text-sm text-muted-foreground">
              {summary.isSuperhost
                ? t("isSuperhost")
                : superhost.meetsRequirements
                  ? t("meetsRequirements")
                  : t("keepGoing")}
            </p>
          </div>
        </div>
        {summary.isSuperhost ? (
          <Badge variant="success">
            <Award aria-hidden="true" /> {t("badge")}
          </Badge>
        ) : (
          <Badge variant="muted">{t("notYet")}</Badge>
        )}
      </div>
      <ul className="grid gap-5 md:grid-cols-3">
        {rows.map((row) => (
          <li key={row.key} className="grid gap-2">
            <div className="flex items-baseline justify-between gap-2 text-sm">
              <span className="font-medium">{row.label}</span>
              <span className="tabular-nums">
                <span className="font-semibold">{row.current}</span>{" "}
                <span className="text-muted-foreground">· {row.target}</span>
              </span>
            </div>
            <ProgressBar value={row.progress} met={row.met} label={row.label} />
          </li>
        ))}
      </ul>
    </section>
  );
}

function PendingRequests() {
  const t = useTranslations("hosting.dashboard");
  const requests = useHostBookings({ page: 1, pageSize: 20, status: BookingStatus.PendingApproval });
  const pending = requests.data?.items.filter((b) => b.status === BookingStatus.PendingApproval) ?? [];

  return (
    <section aria-labelledby="requests-heading" className="grid gap-4">
      <div className="flex items-end justify-between gap-3">
        <h2 id="requests-heading" className="text-xl font-semibold">
          {t("requestsHeading")}
        </h2>
        <Link href="/hosting/reservations?status=pending" className="text-sm font-medium underline underline-offset-4">
          {t("viewAll")}
        </Link>
      </div>
      {requests.isPending ? (
        <BookingsSkeleton count={1} />
      ) : requests.isError ? (
        <HostingError error={requests.error} onRetry={() => requests.refetch()} />
      ) : pending.length === 0 ? (
        <EmptyState icon={Inbox} title={t("noRequestsTitle")} description={t("noRequestsDescription")} className="py-10" />
      ) : (
        <ul className="grid gap-4">
          {pending.map((booking) => (
            <li key={booking.id}>
              <HostBookingCard booking={booking} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function UpcomingStays() {
  const t = useTranslations("hosting.dashboard");
  const bookings = useHostBookings({ page: 1, pageSize: 50 });
  const upcoming = useMemo(() => {
    const now = today();
    return (bookings.data?.items ?? [])
      .filter((b) => b.status === BookingStatus.Confirmed || b.status === BookingStatus.Reserved)
      .filter((b) => {
        const end = parseDateOnly(b.durationEnd);
        return end ? end >= now : false;
      })
      .sort((a, b) => a.durationStart.localeCompare(b.durationStart))
      .slice(0, 6);
  }, [bookings.data]);

  return (
    <section aria-labelledby="upcoming-heading" className="grid gap-4">
      <div className="flex items-end justify-between gap-3">
        <h2 id="upcoming-heading" className="text-xl font-semibold">
          {t("upcomingHeading")}
        </h2>
        <Link href="/hosting/reservations" className="text-sm font-medium underline underline-offset-4">
          {t("viewAll")}
        </Link>
      </div>
      {bookings.isPending ? (
        <BookingsSkeleton count={2} />
      ) : bookings.isError ? (
        <HostingError error={bookings.error} onRetry={() => bookings.refetch()} />
      ) : upcoming.length === 0 ? (
        <EmptyState
          icon={CalendarCheck}
          title={t("noUpcomingTitle")}
          description={t("noUpcomingDescription")}
          className="py-10"
        />
      ) : (
        <ul className="grid gap-4 lg:grid-cols-2">
          {upcoming.map((booking) => (
            <li key={booking.id}>
              <HostBookingCard booking={booking} compact />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function SummarySkeleton() {
  return (
    <div className="grid gap-6" role="status">
      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }, (_, i) => (
          <Skeleton key={i} className="h-28 rounded-2xl" />
        ))}
      </div>
      <Skeleton className="h-44 rounded-2xl" />
    </div>
  );
}

export function BookingsSkeleton({ count = 2 }: { count?: number }) {
  return (
    <div className="grid gap-4" role="status">
      {Array.from({ length: count }, (_, i) => (
        <Skeleton key={i} className="h-36 rounded-2xl" />
      ))}
    </div>
  );
}

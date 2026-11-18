"use client";

import { useQueryClient } from "@tanstack/react-query";
import {
  Banknote,
  CalendarClock,
  CircleCheck,
  CircleDashed,
  Clock,
  ExternalLink,
  Loader2,
  type LucideIcon,
  PauseCircle,
  TriangleAlert,
  Wallet,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef } from "react";
import { toast } from "sonner";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState } from "@/components/shared/state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PaymentModeBadge } from "@/features/payments/components/payment-mode";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatMediumDate, parseDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import { cn } from "@/lib/utils";
import type { Payout, PayoutAccount, PayoutStatus } from "@/types/api";

import { usePayoutAccount, usePayouts, useStartOnboarding } from "../hooks/use-hosting";
import { useHostingSummary } from "../hooks/use-hosting-summary";
import {
  PAYOUT_FILTERS,
  type PayoutFilter,
  PAYOUTS_PAGE_SIZE,
  type PayoutsSearch,
  payoutsHref,
} from "../payouts-search-params";
import { HostingError } from "./hosting-gate";

const STATUS_STYLE: Record<PayoutStatus, { variant: "success" | "info" | "warning" | "destructive" | "muted"; icon: LucideIcon }> = {
  Scheduled: { variant: "info", icon: CalendarClock },
  OnHold: { variant: "warning", icon: PauseCircle },
  Paid: { variant: "success", icon: CircleCheck },
  Failed: { variant: "destructive", icon: TriangleAlert },
  Cancelled: { variant: "muted", icon: XCircle },
};

function PayoutStatusBadge({ status }: { status: PayoutStatus }) {
  const t = useTranslations("hosting.payouts.status");
  const style = STATUS_STYLE[status] ?? STATUS_STYLE.Scheduled;
  const Icon = style.icon;
  return (
    <Badge variant={style.variant} data-status={status}>
      <Icon aria-hidden="true" /> {t(status)}
    </Badge>
  );
}

export function PayoutsView({ search }: { search: PayoutsSearch }) {
  const t = useTranslations("hosting.payouts");
  const router = useRouter();
  const queryClient = useQueryClient();
  const account = usePayoutAccount();
  const handled = useRef(false);

  // Back from onboarding: confirm, refresh the account and payouts, and clean the URL.
  useEffect(() => {
    if (!search.onboarding || handled.current) return;
    handled.current = true;
    if (search.onboarding === "complete") {
      toast.success(t("onboardingComplete"), { description: t("onboardingCompleteDescription") });
    } else {
      toast.info(t("onboardingRefresh"), { description: t("onboardingRefreshDescription") });
    }
    void queryClient.invalidateQueries({ queryKey: ["hosting", "payouts"] });
    void queryClient.invalidateQueries({ queryKey: queryKeys.hosting.summary });
    router.replace(payoutsHref(search), { scroll: false });
  }, [search, router, queryClient, t]);

  return (
    <div className="grid gap-8">
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
        <AccountCard account={account.data} isPending={account.isPending} error={account.error} onRetry={() => account.refetch()} />
        <Totals />
      </div>
      <PayoutsList search={search} pollOnHold={account.data?.status === "Active"} />
    </div>
  );
}

function AccountCard({
  account,
  isPending,
  error,
  onRetry,
}: {
  account: PayoutAccount | undefined;
  isPending: boolean;
  error: Error | null;
  onRetry: () => void;
}) {
  const t = useTranslations("hosting.payouts.account");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const onboarding = useStartOnboarding();

  const onStart = () =>
    onboarding.mutate(undefined, {
      onSuccess: (value) => {
        try {
          const url = new URL(value, window.location.href);
          if (url.origin === window.location.origin) router.push(`${url.pathname}${url.search}`);
          else if (url.protocol === "https:") window.location.assign(url.href);
          else throw new Error("unsafe");
        } catch {
          toast.error(t("onboardingError"));
        }
      },
      onError: (e) => toast.error(t("onboardingError"), { description: errorMessage(e) }),
    });

  if (isPending) return <Skeleton className="h-48 rounded-2xl" />;
  if (error || !account) return <HostingError error={error} title={t("loadError")} onRetry={onRetry} />;

  const active = account.status === "Active";
  const busy = onboarding.isPending || onboarding.isSuccess;

  return (
    <section
      aria-labelledby="payout-account-heading"
      className="grid content-start gap-4 rounded-2xl border bg-card p-6 shadow-card"
      data-testid="payout-account"
      data-status={account.status}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <span
            className={cn(
              "grid size-11 shrink-0 place-items-center rounded-2xl",
              active ? "bg-success/15 text-success" : "bg-accent text-accent-foreground",
            )}
          >
            <Wallet className="size-5" aria-hidden="true" />
          </span>
          <div className="grid gap-0.5">
            <h2 id="payout-account-heading" className="text-lg font-semibold">
              {t("title")}
            </h2>
            <p className="text-sm text-muted-foreground">{t(`text.${account.status}`)}</p>
          </div>
        </div>
        <Badge variant={active ? "success" : account.status === "Pending" ? "warning" : "muted"}>
          {active ? <CircleCheck aria-hidden="true" /> : account.status === "Pending" ? <Clock aria-hidden="true" /> : <CircleDashed aria-hidden="true" />}
          {t(`status.${account.status}`)}
        </Badge>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        {!active && (
          <Button onClick={onStart} disabled={busy}>
            {busy ? <Loader2 className="animate-spin" aria-hidden="true" /> : <ExternalLink aria-hidden="true" />}
            {account.status === "Pending" ? t("continue") : t("start")}
          </Button>
        )}
        <PaymentModeBadge />
      </div>
      {account.mode === "Simulated" && !active && <p className="text-xs text-muted-foreground">{t("simulatedHint")}</p>}
    </section>
  );
}

function Totals() {
  const t = useTranslations("hosting.payouts.totals");
  const locale = useLocale();
  const summary = useHostingSummary();

  if (summary.isPending) return <Skeleton className="h-48 rounded-2xl" />;
  if (summary.isError) return <HostingError error={summary.error} onRetry={() => summary.refetch()} />;

  const { currency } = summary.data;
  const items = [
    { key: "paid", label: t("paid"), value: summary.data.paidTotal, icon: CircleCheck },
    { key: "upcoming", label: t("upcoming"), value: summary.data.upcomingPayouts, icon: CalendarClock },
    { key: "month", label: t("thisMonth"), value: summary.data.earningsThisMonth, icon: Banknote },
  ];
  return (
    <ul className="grid gap-4 sm:grid-cols-3" aria-label={t("label")}>
      {items.map(({ key, label, value, icon: Icon }) => (
        <li key={key} className="grid content-start gap-1.5 rounded-2xl border bg-card p-5 shadow-card" data-testid={`payout-total-${key}`}>
          <span className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
            {label}
            <Icon className="size-4" aria-hidden="true" />
          </span>
          <span className="text-2xl font-semibold tabular-nums">{formatPrice(value, currency, locale)}</span>
        </li>
      ))}
    </ul>
  );
}

function PayoutsList({ search, pollOnHold }: { search: PayoutsSearch; pollOnHold: boolean }) {
  const t = useTranslations("hosting.payouts");
  const locale = useLocale();
  const payouts = usePayouts(
    { page: search.page, pageSize: PAYOUTS_PAGE_SIZE, status: PAYOUT_FILTERS[search.status] },
    { pollOnHold },
  );
  const pages = payouts.data ? Math.ceil(payouts.data.totalCount / PAYOUTS_PAGE_SIZE) : 0;
  const date = (value: string | null) => {
    const parsed = value ? (value.length === 10 ? parseDateOnly(value) : new Date(value)) : undefined;
    return parsed ? formatMediumDate(parsed, locale) : "—";
  };

  return (
    <section aria-labelledby="payouts-heading" className="grid gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 id="payouts-heading" className="text-xl font-semibold">
          {t("heading")}
        </h2>
        <nav aria-label={t("filterLabel")} className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
          {(Object.keys(PAYOUT_FILTERS) as PayoutFilter[]).map((key) => {
            const active = key === search.status;
            const status = PAYOUT_FILTERS[key];
            return (
              <Link
                key={key}
                href={payoutsHref({ status: key, page: 1 })}
                aria-current={active ? "page" : undefined}
                scroll={false}
                className={cn(
                  "inline-flex h-9 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                  active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
                )}
              >
                {status ? t(`status.${status}`) : t("all")}
                {active && payouts.data && (
                  <span className="rounded-full bg-background/20 px-1.5 text-xs tabular-nums">{payouts.data.totalCount}</span>
                )}
              </Link>
            );
          })}
        </nav>
      </div>

      {payouts.isPending ? (
        <Skeleton className="h-64 rounded-2xl" />
      ) : payouts.isError ? (
        <HostingError error={payouts.error} title={t("loadError")} onRetry={() => payouts.refetch()} />
      ) : payouts.data.items.length === 0 ? (
        <EmptyState icon={Banknote} title={t("emptyTitle")} description={t("emptyDescription")} />
      ) : (
        <>
          <div className={cn("overflow-hidden rounded-2xl border bg-card shadow-card", payouts.isPlaceholderData && "opacity-60")}>
            <table className="hidden w-full text-sm md:table" data-testid="payouts-table">
              <thead className="border-b bg-muted/50 text-left text-xs font-semibold tracking-wide text-muted-foreground uppercase">
                <tr>
                  <th scope="col" className="px-4 py-3">{t("columns.listing")}</th>
                  <th scope="col" className="px-4 py-3">{t("columns.guest")}</th>
                  <th scope="col" className="px-4 py-3">{t("columns.scheduled")}</th>
                  <th scope="col" className="px-4 py-3">{t("columns.status")}</th>
                  <th scope="col" className="px-4 py-3">{t("columns.paidOn")}</th>
                  <th scope="col" className="px-4 py-3 text-right">{t("columns.amount")}</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {payouts.data.items.map((payout) => (
                  <tr key={payout.id} data-payout-status={payout.status}>
                    <td className="max-w-56 truncate px-4 py-3 font-medium">{payout.listingName}</td>
                    <td className="px-4 py-3 text-muted-foreground">{payout.guestName}</td>
                    <td className="px-4 py-3 tabular-nums">{date(payout.scheduledFor)}</td>
                    <td className="px-4 py-3">
                      <PayoutStatusBadge status={payout.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground tabular-nums">{date(payout.paidOnUtc)}</td>
                    <td className="px-4 py-3 text-right font-semibold tabular-nums">
                      {formatPrice(payout.amount, payout.currency, locale)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <ul className="divide-y md:hidden">
              {payouts.data.items.map((payout) => (
                <PayoutRow key={payout.id} payout={payout} date={date} />
              ))}
            </ul>
          </div>
          <p className="text-xs text-muted-foreground">{t("feeNote")}</p>
          <Pagination
            page={search.page}
            totalPages={pages}
            pathname="/hosting/payouts"
            searchParams={{ status: search.status !== "all" ? search.status : undefined }}
          />
        </>
      )}
    </section>
  );
}

function PayoutRow({ payout, date }: { payout: Payout; date: (value: string | null) => string }) {
  const t = useTranslations("hosting.payouts");
  const locale = useLocale();
  return (
    <li className="grid gap-1.5 p-4 text-sm">
      <div className="flex items-start justify-between gap-3">
        <span className="font-medium">{payout.listingName}</span>
        <span className="font-semibold tabular-nums">{formatPrice(payout.amount, payout.currency, locale)}</span>
      </div>
      <p className="text-muted-foreground">{payout.guestName}</p>
      <div className="flex flex-wrap items-center gap-2">
        <PayoutStatusBadge status={payout.status} />
        <span className="text-xs text-muted-foreground">
          {payout.paidOnUtc
            ? t("paidOnDate", { date: date(payout.paidOnUtc) })
            : t("scheduledForDate", { date: date(payout.scheduledFor) })}
        </span>
      </div>
    </li>
  );
}

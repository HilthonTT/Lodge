"use client";

import { BellOff, CheckCheck, Loader2, Settings2 } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";

import { useMarkAllNotificationsRead, useNotifications, useUnreadNotificationCount } from "../hooks/use-notifications";
import { NotificationItem } from "./notification-item";

export const NOTIFICATIONS_PAGE_SIZE = 20;

export function NotificationsView({ page, unreadOnly }: { page: number; unreadOnly: boolean }) {
  const t = useTranslations("notifications");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const list = useNotifications({ page, pageSize: NOTIFICATIONS_PAGE_SIZE, unreadOnly });
  const unread = useUnreadNotificationCount();
  const markAll = useMarkAllNotificationsRead();
  const totalPages = list.data ? Math.ceil(list.data.totalCount / NOTIFICATIONS_PAGE_SIZE) : 0;

  const chip = (active: boolean) =>
    cn(
      "inline-flex h-9 items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
      active ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
    );

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <nav aria-label={t("filterLabel")} className="flex gap-2">
          <Link href="/notifications" aria-current={!unreadOnly ? "page" : undefined} className={chip(!unreadOnly)} scroll={false}>
            {t("filterAll")}
          </Link>
          <Link
            href="/notifications?unread=1"
            aria-current={unreadOnly ? "page" : undefined}
            className={chip(unreadOnly)}
            scroll={false}
            data-testid="notifications-unread-filter"
          >
            {t("filterUnread")}
            {(unread.data ?? 0) > 0 && (
              <span className={cn("rounded-full px-1.5 text-xs tabular-nums", unreadOnly ? "bg-background/20" : "bg-muted text-muted-foreground")}>
                {unread.data}
              </span>
            )}
          </Link>
        </nav>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => markAll.mutate()} disabled={!unread.data || markAll.isPending}>
            {markAll.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <CheckCheck aria-hidden="true" />}
            {t("markAllRead")}
          </Button>
          <Button asChild variant="ghost">
            <Link href="/settings/notifications">
              <Settings2 aria-hidden="true" /> {t("preferences")}
            </Link>
          </Button>
        </div>
      </div>

      {list.isPending ? (
        <div className="grid gap-2 rounded-2xl border bg-card p-3" role="status" aria-label={t("loading")}>
          {Array.from({ length: 5 }, (_, i) => (
            <div key={i} className="flex gap-3 p-2">
              <Skeleton className="size-9 rounded-full" />
              <div className="grid flex-1 gap-1.5">
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-3 w-24" />
              </div>
            </div>
          ))}
        </div>
      ) : list.isError ? (
        <ErrorState
          title={t("loadError")}
          description={errorMessage(list.error)}
          action={
            <Button variant="outline" onClick={() => list.refetch()}>
              {tCommon("tryAgain")}
            </Button>
          }
        />
      ) : list.data.items.length === 0 ? (
        <EmptyState icon={BellOff} title={unreadOnly ? t("emptyUnread") : t("empty")} description={t("emptyDescription")} />
      ) : (
        <>
          <ul className={cn("grid gap-1 rounded-2xl border bg-card p-2 shadow-card", list.isPlaceholderData && "opacity-60")}>
            {list.data.items.map((n) => (
              <li key={n.id}>
                <NotificationItem notification={n} />
              </li>
            ))}
          </ul>
          <Pagination
            page={page}
            totalPages={totalPages}
            pathname="/notifications"
            searchParams={{ unread: unreadOnly ? "1" : undefined }}
          />
        </>
      )}
    </div>
  );
}

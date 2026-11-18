"use client";

import { Bell, BellOff, CheckCheck, Loader2 } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { formatNumber } from "@/lib/format";
import { cn } from "@/lib/utils";

import { useMarkAllNotificationsRead, useNotifications, useUnreadNotificationCount } from "../hooks/use-notifications";
import { NotificationItem } from "./notification-item";

/** Count badge for navbar icons ("9+" above nine). */
export function CountBadge({ count, className }: { count: number; className?: string }) {
  const locale = useLocale();
  if (count <= 0) return null;
  return (
    <span
      className={cn(
        "pointer-events-none absolute -top-0.5 -right-0.5 grid h-5 min-w-5 place-items-center rounded-full bg-primary px-1 text-[0.65rem] font-semibold text-primary-foreground tabular-nums ring-2 ring-background",
        className,
      )}
      aria-hidden="true"
    >
      {count > 9 ? "9+" : formatNumber(count, locale)}
    </span>
  );
}

/** Navbar bell: unread badge and a popover with the latest notifications. */
export function NotificationBell() {
  const t = useTranslations("notifications");
  const [open, setOpen] = useState(false);
  const unread = useUnreadNotificationCount();
  const list = useNotifications({ page: 1, pageSize: 8, unreadOnly: false }, { enabled: open });
  const markAll = useMarkAllNotificationsRead();
  const count = unread.data ?? 0;

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="relative rounded-full"
          aria-label={count > 0 ? t("bellUnread", { count }) : t("bell")}
          data-testid="notification-bell"
        >
          <Bell aria-hidden="true" />
          <CountBadge count={count} />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-[min(24rem,calc(100vw-1.5rem))] p-0">
        <div className="flex items-center justify-between gap-2 border-b px-4 py-3">
          <h2 className="font-semibold">{t("title")}</h2>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => markAll.mutate()}
            disabled={count === 0 || markAll.isPending}
            data-testid="notifications-read-all"
          >
            {markAll.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <CheckCheck aria-hidden="true" />}
            {t("markAllRead")}
          </Button>
        </div>
        <div className="max-h-[min(28rem,60dvh)] overflow-y-auto p-1.5">
          {list.isPending ? (
            <div className="grid gap-2 p-2" role="status" aria-label={t("loading")}>
              {Array.from({ length: 3 }, (_, i) => (
                <div key={i} className="flex gap-3">
                  <Skeleton className="size-9 rounded-full" />
                  <div className="grid flex-1 gap-1.5">
                    <Skeleton className="h-4 w-full" />
                    <Skeleton className="h-3 w-1/3" />
                  </div>
                </div>
              ))}
            </div>
          ) : list.isError ? (
            <p className="p-4 text-center text-sm text-muted-foreground">{t("loadError")}</p>
          ) : list.data.items.length === 0 ? (
            <div className="grid justify-items-center gap-2 px-4 py-10 text-center text-sm text-muted-foreground">
              <BellOff className="size-6" aria-hidden="true" />
              {t("empty")}
            </div>
          ) : (
            <ul className="grid gap-0.5">
              {list.data.items.map((n) => (
                <li key={n.id}>
                  <NotificationItem notification={n} onNavigate={() => setOpen(false)} />
                </li>
              ))}
            </ul>
          )}
        </div>
        <div className="border-t p-2">
          <Button asChild variant="ghost" className="w-full">
            <Link href="/notifications" onClick={() => setOpen(false)}>
              {t("seeAll")}
            </Link>
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

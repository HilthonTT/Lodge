"use client";

import { Bell } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { useNowMinute } from "@/features/hosting/hooks/use-now";
import { formatDateTime, formatRelativeTime } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { AppNotification } from "@/types/api";

import { useMarkNotificationRead } from "../hooks/use-notifications";
import { NOTIFICATION_ICONS, notificationHref, useNotificationText } from "../notification-text";

/** One notification: icon, localized text, relative time and an unread dot. Clicking marks it read. */
export function NotificationItem({
  notification,
  onNavigate,
  className,
}: {
  notification: AppNotification;
  /** e.g. close the popover. */
  onNavigate?: () => void;
  className?: string;
}) {
  const t = useTranslations("notifications");
  const locale = useLocale();
  const text = useNotificationText();
  const markRead = useMarkNotificationRead();
  const now = useNowMinute();
  const Icon = NOTIFICATION_ICONS[notification.type] ?? Bell;
  const unread = !notification.readOnUtc;
  const created = new Date(notification.createdOnUtc);

  return (
    <Link
      href={notificationHref(notification.link)}
      onClick={() => {
        if (unread) markRead.mutate(notification);
        onNavigate?.();
      }}
      className={cn(
        "group flex items-start gap-3 rounded-xl px-3 py-3 text-sm transition-colors outline-none hover:bg-muted focus-visible:ring-[3px] focus-visible:ring-ring/40",
        unread && "bg-accent/40",
        className,
      )}
      data-testid="notification-item"
      data-unread={unread || undefined}
    >
      <span
        className={cn(
          "grid size-9 shrink-0 place-items-center rounded-full",
          unread ? "bg-primary/12 text-primary" : "bg-muted text-muted-foreground",
        )}
      >
        <Icon className="size-4.5" aria-hidden="true" />
      </span>
      <span className="grid min-w-0 flex-1 gap-0.5">
        <span className={cn("leading-snug", unread ? "font-medium text-foreground" : "text-muted-foreground")}>
          {text(notification)}
        </span>
        <time dateTime={notification.createdOnUtc} title={formatDateTime(created, locale)} className="text-xs text-muted-foreground">
          {now ? formatRelativeTime(created, locale, now) : formatDateTime(created, locale)}
        </time>
      </span>
      {unread && (
        <span className="mt-1.5 size-2.5 shrink-0 rounded-full bg-primary">
          <span className="sr-only">{t("unread")}</span>
        </span>
      )}
    </Link>
  );
}

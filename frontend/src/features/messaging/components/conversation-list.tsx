"use client";

import { MessagesSquare } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { RemoteImage } from "@/components/shared/remote-image";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useUserId } from "@/features/auth/session-provider";
import { BookingStatusBadge } from "@/features/bookings/components/booking-status-badge";
import { useNowMinute } from "@/features/hosting/hooks/use-now";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, formatShortDate, formatTime, parseDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Conversation } from "@/types/api";

import { useConversations } from "../hooks/use-messaging";
import { toBookingStatus, useSystemEventText } from "../message-text";

/** "14:05" today, "Mon" this week, otherwise "Nov 12". */
function listTime(iso: string, locale: string, now: number) {
  const date = new Date(iso);
  if (!now) return formatShortDate(date, locale);
  const startOfToday = new Date(now);
  startOfToday.setHours(0, 0, 0, 0);
  if (date >= startOfToday) return formatTime(date, locale);
  if (now - date.getTime() < 6 * 86_400_000) return date.toLocaleDateString(locale, { weekday: "short" });
  return formatShortDate(date, locale);
}

export function ConversationList({ selectedId }: { selectedId: string | null }) {
  const t = useTranslations("messaging");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const conversations = useConversations();

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="flex items-center justify-between gap-2 border-b px-4 py-4">
        <h1 className="text-xl font-semibold">{t("title")}</h1>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto p-2">
        {conversations.isPending ? (
          <div className="grid gap-1" role="status" aria-label={t("loading")}>
            {Array.from({ length: 5 }, (_, i) => (
              <div key={i} className="flex gap-3 p-3">
                <Skeleton className="size-12 rounded-full" />
                <div className="grid flex-1 gap-2">
                  <Skeleton className="h-4 w-1/2" />
                  <Skeleton className="h-3 w-3/4" />
                </div>
              </div>
            ))}
          </div>
        ) : conversations.isError ? (
          <ErrorState
            title={t("loadError")}
            description={errorMessage(conversations.error)}
            className="border-0"
            action={
              <Button variant="outline" onClick={() => conversations.refetch()}>
                {tCommon("tryAgain")}
              </Button>
            }
          />
        ) : conversations.data.items.length === 0 ? (
          <EmptyState
            icon={MessagesSquare}
            title={t("emptyTitle")}
            description={t("emptyDescription")}
            className="border-0"
            action={
              <Button asChild variant="outline">
                <Link href="/">{t("explore")}</Link>
              </Button>
            }
          />
        ) : (
          <ul className="grid gap-0.5" data-testid="conversation-list">
            {conversations.data.items.map((c) => (
              <li key={c.id}>
                <ConversationRow conversation={c} selected={c.id === selectedId} />
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function ConversationRow({ conversation: c, selected }: { conversation: Conversation; selected: boolean }) {
  const t = useTranslations("messaging");
  const locale = useLocale();
  const userId = useUserId();
  const now = useNowMinute();
  const systemText = useSystemEventText();
  const unread = c.unreadCount > 0;
  const last = c.lastMessage;
  const preview = !last
    ? t("noMessages")
    : last.type === "System"
      ? systemText(last.systemEvent, last.body)
      : last.senderId === userId
        ? t("youPrefix", { message: last.body })
        : last.body;
  const start = parseDateOnly(c.latestBooking?.durationStart);
  const end = parseDateOnly(c.latestBooking?.durationEnd);

  return (
    <Link
      href={`/messages/${c.id}`}
      aria-current={selected ? "page" : undefined}
      className={cn(
        "flex gap-3 rounded-xl p-3 transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40",
        selected ? "bg-muted" : "hover:bg-muted/60",
      )}
      data-testid="conversation-row"
      data-unread={unread || undefined}
    >
      <span className="relative shrink-0">
        <span className="relative block size-12 overflow-hidden rounded-xl bg-muted">
          <RemoteImage src={c.apartment.imageUrl} alt="" fill sizes="48px" className="object-cover" />
        </span>
        <UserAvatar
          name={c.otherParticipant.firstName}
          src={c.otherParticipant.avatarUrl}
          className="absolute -right-1.5 -bottom-1.5 size-7 ring-2 ring-background"
          fallbackClassName="text-[0.65rem]"
        />
      </span>
      <span className="grid min-w-0 flex-1 gap-0.5">
        <span className="flex items-baseline justify-between gap-2">
          <span className="flex min-w-0 items-baseline gap-1.5">
            <span className={cn("truncate", unread ? "font-semibold" : "font-medium")}>{c.otherParticipant.firstName}</span>
            <span className="shrink-0 text-xs text-muted-foreground">
              {c.otherParticipant.role === "Host" ? t("roleHost") : t("roleGuest")}
            </span>
          </span>
          <time dateTime={c.lastMessageOnUtc} className={cn("shrink-0 text-xs", unread ? "font-semibold text-foreground" : "text-muted-foreground")}>
            {listTime(c.lastMessageOnUtc, locale, now)}
          </time>
        </span>
        <span className="truncate text-xs text-muted-foreground">{c.apartment.name}</span>
        <span className="flex items-center gap-2">
          <span className={cn("line-clamp-1 flex-1 text-sm", unread ? "font-medium text-foreground" : "text-muted-foreground", last?.type === "System" && "italic")}>
            {preview}
          </span>
          {unread && (
            <span className="size-2.5 shrink-0 rounded-full bg-primary">
              <span className="sr-only">{t("unreadCount", { count: c.unreadCount })}</span>
            </span>
          )}
        </span>
        {c.latestBooking && (
          <span className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1">
            <BookingStatusBadge status={toBookingStatus(c.latestBooking.status)} />
            {start && end && <span className="text-xs text-muted-foreground">{formatDateRange(start, end, locale)}</span>}
          </span>
        )}
      </span>
    </Link>
  );
}

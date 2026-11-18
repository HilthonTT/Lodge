"use client";

import { AlertCircle, ArrowLeft, Info, Loader2, RotateCw, Trash2, X } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Fragment, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";

import { ErrorState } from "@/components/shared/state";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useUserId } from "@/features/auth/session-provider";
import { useNowMinute } from "@/features/hosting/hooks/use-now";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateTime, formatLongDay, formatTime, toDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Conversation } from "@/types/api";

import {
  type ThreadMessage,
  useConversation,
  useMarkConversationRead,
  useSendMessage,
  useThread,
} from "../hooks/use-messaging";
import { usePageVisible, useSystemEventText } from "../message-text";
import { Composer } from "./composer";
import { ConversationBookingPanel } from "./conversation-booking-panel";

export function ThreadView({ conversationId }: { conversationId: string }) {
  const t = useTranslations("messaging");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const conversation = useConversation(conversationId);
  const [panelOpen, setPanelOpen] = useState(false);

  if (conversation.isError && !conversation.data) {
    return (
      <div className="grid flex-1 place-items-center p-6">
        <ErrorState
          title={conversation.error.isNotFound ? t("notFoundTitle") : t("threadLoadError")}
          description={conversation.error.isNotFound ? t("notFoundDescription") : errorMessage(conversation.error)}
          action={
            <Button asChild variant="outline">
              <Link href="/messages">{t("backToInbox")}</Link>
            </Button>
          }
        />
      </div>
    );
  }

  const c = conversation.data;

  return (
    <div className="flex min-h-0 flex-1">
      <div className="flex min-h-0 min-w-0 flex-1 flex-col">
        <header className="flex items-center gap-3 border-b px-3 py-3 sm:px-4">
          <Button asChild variant="ghost" size="icon" className="shrink-0 rounded-full lg:hidden">
            <Link href="/messages" aria-label={t("backToInbox")}>
              <ArrowLeft aria-hidden="true" />
            </Link>
          </Button>
          {c ? (
            <>
              <UserAvatar name={c.otherParticipant.firstName} src={c.otherParticipant.avatarUrl} className="size-10" />
              <div className="grid min-w-0 flex-1">
                <h2 className="truncate font-semibold" data-testid="thread-title">
                  {c.otherParticipant.firstName}
                  <span className="ml-2 text-xs font-normal text-muted-foreground">
                    {c.otherParticipant.role === "Host" ? t("roleHost") : t("roleGuest")}
                  </span>
                </h2>
                <p className="truncate text-sm text-muted-foreground">{c.apartment.name}</p>
              </div>
            </>
          ) : (
            <div className="flex flex-1 items-center gap-3">
              <Skeleton className="size-10 rounded-full" />
              <Skeleton className="h-4 w-40" />
            </div>
          )}
          <Button
            variant={panelOpen ? "secondary" : "ghost"}
            size="sm"
            className="shrink-0 rounded-full xl:hidden"
            onClick={() => setPanelOpen((v) => !v)}
            aria-expanded={panelOpen}
            aria-controls="conversation-details"
          >
            {panelOpen ? <X aria-hidden="true" /> : <Info aria-hidden="true" />}
            {t("details")}
          </Button>
        </header>
        {panelOpen && c ? (
          <div id="conversation-details" className="min-h-0 flex-1 overflow-y-auto xl:hidden">
            <ConversationBookingPanel conversation={c} />
          </div>
        ) : (
          <MessageThread conversationId={conversationId} conversation={c} />
        )}
        {conversation.isError && (
          <p className="px-4 py-2 text-xs text-destructive">
            {errorMessage(conversation.error)}{" "}
            <button type="button" className="underline" onClick={() => conversation.refetch()}>
              {tCommon("tryAgain")}
            </button>
          </p>
        )}
      </div>
      <aside aria-label={t("details")} className="hidden w-72 shrink-0 overflow-y-auto border-l xl:block">
        {c ? <ConversationBookingPanel conversation={c} /> : <Skeleton className="m-5 aspect-[4/3] rounded-2xl" />}
      </aside>
    </div>
  );
}

type DayGroup = { day: string; date: Date; messages: ThreadMessage[] };

function groupByDay(messages: ThreadMessage[]): DayGroup[] {
  const groups: DayGroup[] = [];
  for (const message of messages) {
    const date = new Date(message.sentOnUtc);
    const day = toDateOnly(date);
    const last = groups.at(-1);
    if (last?.day === day) last.messages.push(message);
    else groups.push({ day, date, messages: [message] });
  }
  return groups;
}

/** Messages with day separators; loads older pages when scrolled to the top and marks the thread read. */
function MessageThread({ conversationId, conversation }: { conversationId: string; conversation: Conversation | undefined }) {
  const t = useTranslations("messaging");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const userId = useUserId();
  const now = useNowMinute();
  const thread = useThread(conversationId);
  const { send, discard } = useSendMessage(conversationId);
  const markRead = useMarkConversationRead();
  const visible = usePageVisible();
  const systemText = useSystemEventText();

  const scroller = useRef<HTMLDivElement>(null);
  const topSentinel = useRef<HTMLDivElement>(null);
  const atBottom = useRef(true);
  const previous = useRef<{ first?: string; last?: string; height: number }>({ height: 0 });

  // Pages come newest first; each page is oldest first.
  const messages = useMemo(
    () => (thread.data ? [...thread.data.pages].reverse().flatMap((page) => page.items) : []),
    [thread.data],
  );
  const groups = useMemo(() => groupByDay(messages), [messages]);

  // Keep the reading position: jump to the bottom on load and for new messages (when already
  // at the bottom, or when it's our own), and keep the view still when older pages load above.
  useLayoutEffect(() => {
    const el = scroller.current;
    if (!el) return;
    const first = messages[0]?.id;
    const last = messages.at(-1)?.id;
    const prev = previous.current;
    if (prev.first && first !== prev.first && last === prev.last) {
      el.scrollTop += el.scrollHeight - prev.height;
    } else if (last !== prev.last) {
      const mine = messages.at(-1)?.senderId === userId;
      if (!prev.last || atBottom.current || mine) el.scrollTop = el.scrollHeight;
    }
    previous.current = { first, last, height: el.scrollHeight };
  }, [messages, userId]);

  // Older pages when the top comes into view.
  const { hasNextPage, isFetchingNextPage, fetchNextPage } = thread;
  useEffect(() => {
    const root = scroller.current;
    const target = topSentinel.current;
    if (!root || !target || !hasNextPage) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting) && !isFetchingNextPage) void fetchNextPage();
      },
      { root, rootMargin: "120px 0px 0px 0px" },
    );
    observer.observe(target);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage, thread.isSuccess]);

  // Mark read while the thread is open and the tab visible (again whenever a new message arrives).
  const lastIncoming = useMemo(
    () => messages.findLast((m) => !m.pending && m.senderId !== userId)?.id,
    [messages, userId],
  );
  const unread = conversation?.unreadCount ?? 0;
  const markedFor = useRef<string | undefined>(undefined);
  const { mutate: markReadMutate } = markRead;
  useEffect(() => {
    if (!visible || !thread.isSuccess) return;
    if (unread === 0 && markedFor.current === lastIncoming) return;
    if (unread === 0 && markedFor.current === undefined) {
      markedFor.current = lastIncoming;
      return;
    }
    markedFor.current = lastIncoming;
    markReadMutate(conversationId);
  }, [visible, unread, lastIncoming, conversationId, markReadMutate, thread.isSuccess]);

  const onSend = (body: string) => {
    atBottom.current = true;
    void send(body);
  };

  const dayLabel = (group: DayGroup) => {
    const today = now ? toDateOnly(new Date(now)) : null;
    const yesterday = now ? toDateOnly(new Date(now - 86_400_000)) : null;
    if (group.day === today) return t("today");
    if (group.day === yesterday) return t("yesterday");
    return formatLongDay(group.date, locale);
  };

  return (
    <>
      <div
        ref={scroller}
        onScroll={(e) => {
          const el = e.currentTarget;
          atBottom.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80;
        }}
        className="min-h-0 flex-1 overflow-y-auto px-3 py-4 sm:px-6"
        role="log"
        aria-live="polite"
        aria-label={t("threadLabel")}
        data-testid="thread"
      >
        {thread.isPending ? (
          <div className="grid gap-4" role="status" aria-label={t("loading")}>
            {Array.from({ length: 4 }, (_, i) => (
              <Skeleton key={i} className={cn("h-14 w-2/3 rounded-2xl", i % 2 && "justify-self-end")} />
            ))}
          </div>
        ) : thread.isError ? (
          <ErrorState
            title={t("threadLoadError")}
            description={errorMessage(thread.error)}
            action={
              <Button variant="outline" onClick={() => thread.refetch()}>
                {tCommon("tryAgain")}
              </Button>
            }
          />
        ) : (
          <>
            <div ref={topSentinel} className="h-px" aria-hidden="true" />
            {isFetchingNextPage && (
              <p className="flex justify-center py-2 text-muted-foreground" role="status">
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                <span className="sr-only">{t("loadingOlder")}</span>
              </p>
            )}
            {!hasNextPage && messages.length > 0 && (
              <p className="pb-4 text-center text-xs text-muted-foreground">{t("threadStart")}</p>
            )}
            {groups.map((group) => (
              <section key={group.day} aria-label={dayLabel(group)} className="grid gap-1.5">
                <div className="sticky top-0 z-10 flex justify-center py-2">
                  <span className="rounded-full bg-muted/90 px-3 py-1 text-xs font-medium text-muted-foreground backdrop-blur">
                    {dayLabel(group)}
                  </span>
                </div>
                {group.messages.map((message, index) => {
                  const prev = group.messages[index - 1];
                  if (message.type === "System") {
                    return (
                      <p
                        key={message.id}
                        className="mx-auto my-2 max-w-md text-center text-xs text-muted-foreground"
                        data-testid="system-message"
                      >
                        <span className="font-medium">{systemText(message.systemEvent, message.body)}</span>
                        {" · "}
                        <time dateTime={message.sentOnUtc}>{formatTime(new Date(message.sentOnUtc), locale)}</time>
                      </p>
                    );
                  }
                  const mine = message.senderId === userId;
                  const startsRun = !prev || prev.senderId !== message.senderId || prev.type === "System";
                  return (
                    <Fragment key={message.id}>
                      <MessageBubble
                        message={message}
                        mine={mine}
                        showAvatar={!mine && startsRun}
                        conversation={conversation}
                        onRetry={() => void send(message.body, message.id)}
                        onDiscard={() => discard(message.id)}
                      />
                    </Fragment>
                  );
                })}
              </section>
            ))}
            {messages.length === 0 && <p className="py-10 text-center text-sm text-muted-foreground">{t("noMessages")}</p>}
          </>
        )}
      </div>
      <Composer onSend={onSend} disabled={!thread.isSuccess} />
    </>
  );
}

function MessageBubble({
  message,
  mine,
  showAvatar,
  conversation,
  onRetry,
  onDiscard,
}: {
  message: ThreadMessage;
  mine: boolean;
  showAvatar: boolean;
  conversation: Conversation | undefined;
  onRetry: () => void;
  onDiscard: () => void;
}) {
  const t = useTranslations("messaging");
  const locale = useLocale();
  const sent = new Date(message.sentOnUtc);

  return (
    <div
      className={cn("flex items-end gap-2", mine ? "justify-end" : "justify-start", showAvatar && "mt-2")}
      data-testid="message"
      data-mine={mine || undefined}
      data-pending={message.pending}
    >
      {!mine && (
        <span className="w-8 shrink-0">
          {showAvatar && conversation && (
            <UserAvatar
              name={conversation.otherParticipant.firstName}
              src={conversation.otherParticipant.avatarUrl}
              className="size-8"
              fallbackClassName="text-xs"
            />
          )}
        </span>
      )}
      <div className={cn("grid max-w-[min(32rem,80%)] gap-1", mine && "justify-items-end")}>
        <p
          className={cn(
            "rounded-2xl px-3.5 py-2 text-sm leading-relaxed break-words whitespace-pre-wrap",
            mine ? "rounded-br-md bg-primary text-primary-foreground" : "rounded-bl-md bg-muted",
            message.pending === "sending" && "opacity-60",
            message.pending === "failed" && "bg-destructive/10 text-foreground ring-1 ring-destructive/40",
          )}
        >
          {message.body}
        </p>
        {message.pending === "failed" ? (
          <span className="flex items-center gap-2 text-xs text-destructive" role="alert">
            <AlertCircle className="size-3.5" aria-hidden="true" />
            {t("notSent")}
            <button type="button" onClick={onRetry} className="inline-flex items-center gap-1 font-medium underline-offset-2 hover:underline">
              <RotateCw className="size-3" aria-hidden="true" /> {t("retry")}
            </button>
            <button
              type="button"
              onClick={onDiscard}
              className="inline-flex items-center gap-1 text-muted-foreground hover:text-foreground"
              aria-label={t("discard")}
            >
              <Trash2 className="size-3" aria-hidden="true" />
            </button>
          </span>
        ) : (
          <time dateTime={message.sentOnUtc} title={formatDateTime(sent, locale)} className="px-1 text-[0.7rem] text-muted-foreground">
            {message.pending === "sending" ? t("sending") : formatTime(sent, locale)}
          </time>
        )}
      </div>
    </div>
  );
}

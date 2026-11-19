"use client";

import {
  type HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type RetryContext,
} from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { usePathname, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { createContext, use, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { useSession } from "@/features/auth/session-provider";
import { appendMessageToThread, bumpConversation } from "@/features/messaging/hooks/use-messaging";
import { notificationHref, useNotificationText } from "@/features/notifications/notification-text";
import { prependNotification } from "@/features/notifications/hooks/use-notifications";
import { readToken } from "@/lib/auth/token.client";
import { env } from "@/lib/env";
import { queryKeys } from "@/lib/query-keys";
import type { AppNotification, Message, UnreadCounts } from "@/types/api";

export const HUB_URL = `${env.NEXT_PUBLIC_API_BASE_URL}/hubs/notifications`;

/** Polling interval for counts and lists while the hub is unavailable. */
export const FALLBACK_POLL_MS = 60_000;

export type RealtimeStatus = "off" | "connecting" | "connected" | "reconnecting" | "disconnected";

const RealtimeContext = createContext<RealtimeStatus>("off");

/** 1 s, 2 s, 4 s … capped at 30 s, with jitter so tabs don't reconnect in lockstep. */
function backoff(attempt: number) {
  const base = Math.min(30_000, 1_000 * 2 ** Math.min(attempt, 5));
  return base + Math.round(Math.random() * 0.3 * base);
}

/**
 * One SignalR connection per signed-in tab. Hub events are written straight into
 * the TanStack Query cache; when the hub is down, queries fall back to polling
 * (see `usePollingInterval`). The connection is restarted whenever the token
 * changes (sign-in, refresh) and stopped on sign-out.
 */
export function RealtimeProvider({ children }: { children: React.ReactNode }) {
  const { session } = useSession();
  const queryClient = useQueryClient();
  const router = useRouter();
  const pathname = usePathname();
  const t = useTranslations("notifications");
  const notificationText = useNotificationText();
  const [status, setStatus] = useState<RealtimeStatus>("off");

  // Handlers read the latest values without restarting the connection.
  const latest = useRef({ queryClient, router, pathname, t, notificationText, userId: session?.userId ?? null });
  useEffect(() => {
    latest.current = { queryClient, router, pathname, t, notificationText, userId: session?.userId ?? null };
  });

  // A new token (sign-in or refresh) means a new connection.
  const tokenKey = session ? `${session.userId}:${session.expiresAt}` : null;

  useEffect(() => {
    if (!tokenKey) return;

    let disposed = false;
    let retryTimer: number | undefined;
    let attempt = 0;

    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => readToken() ?? "", withCredentials: true })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (ctx: RetryContext) => backoff(ctx.previousRetryCount) })
      .configureLogging(process.env.NODE_ENV === "production" ? LogLevel.None : LogLevel.Warning)
      .build();

    connection.on("unreadCounts", (counts: UnreadCounts) => {
      const { queryClient: qc } = latest.current;
      qc.setQueryData(queryKeys.messaging.unreadCount, counts.messages);
      qc.setQueryData(queryKeys.notifications.unreadCount, counts.notifications);
    });

    connection.on("messageReceived", ({ message }: { conversationId: string; message: Message }) => {
      const { queryClient: qc, userId } = latest.current;
      appendMessageToThread(qc, message);
      const known = bumpConversation(qc, message, { unread: message.senderId !== null && message.senderId !== userId });
      if (!known) void qc.invalidateQueries({ queryKey: queryKeys.messaging.conversations });
      // A system message means the booking changed: refresh the side panel and trips.
      if (message.type === "System") {
        void qc.invalidateQueries({ queryKey: queryKeys.messaging.conversation(message.conversationId) });
        void qc.invalidateQueries({ queryKey: queryKeys.bookings.all });
      }
    });

    connection.on("conversationUpdated", ({ conversationId }: { conversationId: string }) => {
      const { queryClient: qc } = latest.current;
      void qc.invalidateQueries({ queryKey: queryKeys.messaging.conversations, exact: false });
      void qc.invalidateQueries({ queryKey: queryKeys.messaging.conversation(conversationId) });
    });

    connection.on("notificationReceived", ({ notification }: { notification: AppNotification }) => {
      const { queryClient: qc, router: r, pathname: path, t: tn, notificationText: text } = latest.current;
      prependNotification(qc, notification);
      // Something changed for a booking or a payout: refresh whatever shows it.
      void qc.invalidateQueries({ queryKey: queryKeys.bookings.all });
      void qc.invalidateQueries({ queryKey: queryKeys.hosting.all });
      void qc.invalidateQueries({ queryKey: queryKeys.experienceBookings.all });
      if (path !== "/notifications") {
        const href = notificationHref(notification.link);
        toast(text(notification), {
          id: notification.id,
          duration: 6_000,
          action: { label: tn("view"), onClick: () => r.push(href) },
        });
      }
    });

    connection.onreconnecting(() => !disposed && setStatus("reconnecting"));
    connection.onreconnected(() => {
      if (disposed) return;
      setStatus("connected");
      // Catch up on anything missed while offline.
      void latest.current.queryClient.invalidateQueries({ queryKey: queryKeys.messaging.all });
      void latest.current.queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all });
    });
    connection.onclose(() => {
      if (disposed) return;
      setStatus("disconnected");
      scheduleStart();
    });

    function scheduleStart() {
      window.clearTimeout(retryTimer);
      retryTimer = window.setTimeout(start, backoff(attempt++));
    }

    async function start() {
      if (disposed || connection.state !== HubConnectionState.Disconnected) return;
      setStatus((s) => (s === "connected" ? s : "connecting"));
      try {
        await connection.start();
        if (disposed) return;
        attempt = 0;
        setStatus("connected");
        // Polling stops once connected: fetch anything that arrived since the last poll or the previous connection.
        void latest.current.queryClient.invalidateQueries({ queryKey: queryKeys.messaging.all });
        void latest.current.queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all });
      } catch {
        // Hub unreachable (or the effect was cleaned up mid-negotiation): poll and retry later.
        if (disposed) return;
        setStatus("disconnected");
        scheduleStart();
      }
    }

    // Deferred a tick: React Strict Mode mounts effects twice in development, and a
    // connection stopped mid-negotiation logs a noisy error.
    retryTimer = window.setTimeout(start, 0);

    return () => {
      disposed = true;
      window.clearTimeout(retryTimer);
      void connection.stop().catch(() => undefined);
    };
  }, [tokenKey]);

  return <RealtimeContext value={tokenKey ? status : "off"}>{children}</RealtimeContext>;
}

export function useRealtimeStatus() {
  return use(RealtimeContext);
}

/** `refetchInterval` for live data: off while the hub is connected, 60 s otherwise. */
export function usePollingInterval(): number | false {
  const status = use(RealtimeContext);
  return status === "connected" ? false : FALLBACK_POLL_MS;
}

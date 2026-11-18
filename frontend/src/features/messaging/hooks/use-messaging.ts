"use client";

import {
  type InfiniteData,
  keepPreviousData,
  type QueryClient,
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useCallback } from "react";

import { useSession } from "@/features/auth/session-provider";
import { usePollingInterval } from "@/features/realtime/realtime-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import { uuid } from "@/lib/uuid";
import type { Conversation, Message, MessagesPage, PagedList } from "@/types/api";

import { messagingApi } from "../api/messaging";

/** A message in the thread cache; `pending` marks optimistic sends. */
export type ThreadMessage = Message & { pending?: "sending" | "failed" };
type ThreadPage = Omit<MessagesPage, "items"> & { items: ThreadMessage[] };
export type ThreadData = InfiniteData<ThreadPage, string | undefined>;

export const CONVERSATIONS_PAGE_SIZE = 50;
const LIST_PARAMS = { page: 1, pageSize: CONVERSATIONS_PAGE_SIZE };

/* ------------------------------------------------------------------ */
/* Cache helpers (shared with the real-time provider)                  */
/* ------------------------------------------------------------------ */

/** Appends a message to the newest page of a cached thread (no-op when the thread isn't cached or has it). */
export function appendMessageToThread(queryClient: QueryClient, message: ThreadMessage) {
  queryClient.setQueryData<ThreadData>(queryKeys.messaging.messages(message.conversationId), (data) => {
    if (!data || data.pages.length === 0) return data;
    if (data.pages.some((page) => page.items.some((m) => m.id === message.id))) return data;
    // Our own message arriving through the hub before the POST answered: swap it for the
    // optimistic copy instead of showing it twice.
    const optimistic = data.pages
      .flatMap((page) => page.items)
      .find((m) => m.pending === "sending" && m.senderId === message.senderId && m.body === message.body);
    if (optimistic && !message.pending) {
      return {
        ...data,
        pages: data.pages.map((page) => ({
          ...page,
          items: page.items.map((m) => (m.id === optimistic.id ? message : m)),
        })),
      };
    }
    const [newest, ...older] = data.pages;
    return { ...data, pages: [{ ...newest!, items: [...newest!.items, message] }, ...older] };
  });
}

function replaceInThread(queryClient: QueryClient, conversationId: string, tempId: string, next: ThreadMessage | null) {
  queryClient.setQueryData<ThreadData>(queryKeys.messaging.messages(conversationId), (data) => {
    if (!data) return data;
    // The real-time event may already have delivered the saved message: then just drop the temp one.
    const delivered = next && !next.pending && data.pages.some((p) => p.items.some((m) => m.id === next.id));
    return {
      ...data,
      pages: data.pages.map((page) => ({
        ...page,
        items: page.items.flatMap((m) => (m.id === tempId ? (next && !delivered ? [next] : []) : [m])),
      })),
    };
  });
}

/** Moves a conversation to the top of every cached list with its new last message. */
export function bumpConversation(queryClient: QueryClient, message: Message, options: { unread?: boolean } = {}) {
  let found = false;
  queryClient.setQueriesData<PagedList<Conversation>>({ queryKey: [...queryKeys.messaging.conversations, "list"] }, (list) => {
    if (!list) return list;
    const current = list.items.find((c) => c.id === message.conversationId);
    if (!current) return list;
    found = true;
    const updated: Conversation = {
      ...current,
      lastMessage: {
        body: message.body,
        type: message.type,
        sentOnUtc: message.sentOnUtc,
        senderId: message.senderId,
        systemEvent: message.systemEvent,
      },
      lastMessageOnUtc: message.sentOnUtc,
      unreadCount: options.unread ? current.unreadCount + 1 : current.unreadCount,
    };
    return { ...list, items: [updated, ...list.items.filter((c) => c.id !== current.id)] };
  });
  return found;
}

/** Clears the unread count of a conversation in every cached list and its detail. */
function clearUnread(queryClient: QueryClient, conversationId: string) {
  queryClient.setQueriesData<PagedList<Conversation>>({ queryKey: [...queryKeys.messaging.conversations, "list"] }, (list) =>
    list ? { ...list, items: list.items.map((c) => (c.id === conversationId ? { ...c, unreadCount: 0 } : c)) } : list,
  );
  queryClient.setQueryData<Conversation>(queryKeys.messaging.conversation(conversationId), (c) =>
    c ? { ...c, unreadCount: 0 } : c,
  );
}

/* ------------------------------------------------------------------ */
/* Queries                                                             */
/* ------------------------------------------------------------------ */

export function useConversations() {
  const { isAuthenticated } = useSession();
  const refetchInterval = usePollingInterval();
  return useQuery<PagedList<Conversation>, ApiError>({
    queryKey: queryKeys.messaging.conversationsList(LIST_PARAMS),
    queryFn: () => messagingApi.list(getBrowserHttp(), LIST_PARAMS),
    enabled: isAuthenticated,
    placeholderData: keepPreviousData,
    staleTime: 10_000,
    refetchInterval,
  });
}

export function useConversation(id: string) {
  const queryClient = useQueryClient();
  return useQuery<Conversation, ApiError>({
    queryKey: queryKeys.messaging.conversation(id),
    queryFn: () => messagingApi.get(getBrowserHttp(), id),
    staleTime: 10_000,
    // Show the row from the inbox list while the detail loads.
    placeholderData: () =>
      queryClient
        .getQueryData<PagedList<Conversation>>(queryKeys.messaging.conversationsList(LIST_PARAMS))
        ?.items.find((c) => c.id === id),
  });
}

export function useThread(id: string) {
  const refetchInterval = usePollingInterval();
  return useInfiniteQuery<ThreadPage, ApiError, ThreadData, readonly unknown[], string | undefined>({
    queryKey: queryKeys.messaging.messages(id),
    queryFn: ({ pageParam }) => messagingApi.messages(getBrowserHttp(), id, { before: pageParam }),
    initialPageParam: undefined,
    getNextPageParam: (last) => (last.hasMore && last.nextBefore ? last.nextBefore : undefined),
    staleTime: Infinity,
    // Without the hub, re-fetch every page once a minute (cheap: threads are small).
    refetchInterval,
  });
}

export function useUnreadMessageCount() {
  const { isAuthenticated } = useSession();
  const refetchInterval = usePollingInterval();
  return useQuery<number, ApiError>({
    queryKey: queryKeys.messaging.unreadCount,
    queryFn: () => messagingApi.unreadCount(getBrowserHttp()),
    enabled: isAuthenticated,
    staleTime: 30_000,
    refetchInterval,
  });
}

/* ------------------------------------------------------------------ */
/* Mutations                                                           */
/* ------------------------------------------------------------------ */

export function useStartConversation() {
  const queryClient = useQueryClient();
  return useMutation<{ conversationId: string }, ApiError, { apartmentId: string; message: string }>({
    mutationFn: ({ apartmentId, message }) => messagingApi.start(getBrowserHttp(), apartmentId, message),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.messaging.all }),
  });
}

/**
 * Optimistic send: the message shows immediately (greyed out), is swapped for the
 * saved one when the API answers, or is marked failed with a retry button.
 */
export function useSendMessage(conversationId: string) {
  const queryClient = useQueryClient();
  const userId = useSession().session?.userId ?? null;

  const send = useCallback(
    async (body: string, retryOf?: string) => {
      const tempId = retryOf ?? `temp-${uuid()}`;
      const optimistic: ThreadMessage = {
        id: tempId,
        conversationId,
        senderId: userId,
        type: "User",
        body,
        systemEvent: null,
        bookingId: null,
        sentOnUtc: new Date().toISOString(),
        pending: "sending",
      };
      if (retryOf) replaceInThread(queryClient, conversationId, tempId, optimistic);
      else appendMessageToThread(queryClient, optimistic);
      try {
        const saved = await messagingApi.send(getBrowserHttp(), conversationId, body);
        replaceInThread(queryClient, conversationId, tempId, saved);
        bumpConversation(queryClient, saved);
        clearUnread(queryClient, conversationId);
        return true;
      } catch {
        replaceInThread(queryClient, conversationId, tempId, { ...optimistic, pending: "failed" });
        return false;
      }
    },
    [conversationId, queryClient, userId],
  );

  const discard = useCallback(
    (tempId: string) => replaceInThread(queryClient, conversationId, tempId, null),
    [conversationId, queryClient],
  );

  return { send, discard };
}

export function useMarkConversationRead() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (id) => messagingApi.markRead(getBrowserHttp(), id),
    onMutate: (id) => clearUnread(queryClient, id),
    // The hub pushes `unreadCounts`; refetch the badge anyway when it is down.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.messaging.unreadCount }),
  });
}

/**
 * Finds the conversation for a listing (and, for hosts, a guest) to open from a
 * booking. Returns null when there is none yet.
 */
export function useFindConversation() {
  const queryClient = useQueryClient();
  return useCallback(
    async (apartmentId: string, otherUserId?: string) => {
      const list = await queryClient.fetchQuery({
        queryKey: queryKeys.messaging.conversationsList({ page: 1, pageSize: 100 }),
        queryFn: () => messagingApi.list(getBrowserHttp(), { page: 1, pageSize: 100 }),
        staleTime: 10_000,
      });
      return (
        list.items.find(
          (c) => c.apartment.id === apartmentId && (!otherUserId || c.otherParticipant.id === otherUserId),
        ) ?? null
      );
    },
    [queryClient],
  );
}

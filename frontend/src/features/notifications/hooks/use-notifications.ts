"use client";

import { keepPreviousData, type QueryClient, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession, useUserId } from "@/features/auth/session-provider";
import { usePollingInterval } from "@/features/realtime/realtime-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { AppNotification, NotificationPreferences, PagedList } from "@/types/api";

import { notificationsApi, type NotificationsParams } from "../api/notifications";

type ListSnapshot = [readonly unknown[], PagedList<AppNotification> | undefined][];

/** Adds a new notification to the first page of every cached list. */
export function prependNotification(queryClient: QueryClient, notification: AppNotification) {
  queryClient.setQueriesData<PagedList<AppNotification>>({ queryKey: queryKeys.notifications.lists }, (list) => {
    if (!list || list.page !== 1 || list.items.some((n) => n.id === notification.id)) return list;
    return {
      ...list,
      totalCount: list.totalCount + 1,
      items: [notification, ...list.items].slice(0, list.pageSize),
      hasNextPage: list.hasNextPage || list.items.length >= list.pageSize,
    };
  });
}

function markReadInLists(queryClient: QueryClient, id: string | null) {
  const now = new Date().toISOString();
  queryClient.setQueriesData<PagedList<AppNotification>>({ queryKey: queryKeys.notifications.lists }, (list) =>
    list
      ? {
          ...list,
          items: list.items.map((n) => ((id === null || n.id === id) && !n.readOnUtc ? { ...n, readOnUtc: now } : n)),
        }
      : list,
  );
}

export function useNotifications(params: NotificationsParams, options: { enabled?: boolean } = {}) {
  const { isAuthenticated } = useSession();
  const refetchInterval = usePollingInterval();
  return useQuery<PagedList<AppNotification>, ApiError>({
    queryKey: queryKeys.notifications.list(params),
    queryFn: () => notificationsApi.list(getBrowserHttp(), params),
    enabled: isAuthenticated && (options.enabled ?? true),
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    refetchInterval,
  });
}

export function useUnreadNotificationCount() {
  const { isAuthenticated } = useSession();
  const refetchInterval = usePollingInterval();
  return useQuery<number, ApiError>({
    queryKey: queryKeys.notifications.unreadCount,
    queryFn: () => notificationsApi.unreadCount(getBrowserHttp()),
    enabled: isAuthenticated,
    staleTime: 30_000,
    refetchInterval,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, AppNotification, { lists: ListSnapshot; count: number | undefined }>({
    mutationFn: (n) => notificationsApi.markRead(getBrowserHttp(), n.id),
    onMutate: async (n) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.notifications.all });
      const lists = queryClient.getQueriesData<PagedList<AppNotification>>({ queryKey: queryKeys.notifications.lists });
      const count = queryClient.getQueryData<number>(queryKeys.notifications.unreadCount);
      markReadInLists(queryClient, n.id);
      if (!n.readOnUtc && count !== undefined) {
        queryClient.setQueryData(queryKeys.notifications.unreadCount, Math.max(0, count - 1));
      }
      return { lists, count };
    },
    onError: (_e, _n, context) => {
      context?.lists.forEach(([key, data]) => queryClient.setQueryData(key, data));
      if (context?.count !== undefined) queryClient.setQueryData(queryKeys.notifications.unreadCount, context.count);
    },
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, void, { lists: ListSnapshot; count: number | undefined }>({
    mutationFn: () => notificationsApi.markAllRead(getBrowserHttp()),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: queryKeys.notifications.all });
      const lists = queryClient.getQueriesData<PagedList<AppNotification>>({ queryKey: queryKeys.notifications.lists });
      const count = queryClient.getQueryData<number>(queryKeys.notifications.unreadCount);
      markReadInLists(queryClient, null);
      queryClient.setQueryData(queryKeys.notifications.unreadCount, 0);
      return { lists, count };
    },
    onError: (_e, _v, context) => {
      context?.lists.forEach(([key, data]) => queryClient.setQueryData(key, data));
      if (context?.count !== undefined) queryClient.setQueryData(queryKeys.notifications.unreadCount, context.count);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications.lists }),
  });
}

export function useNotificationPreferences() {
  const userId = useUserId();
  return useQuery<NotificationPreferences, ApiError>({
    queryKey: queryKeys.notifications.preferences(userId ?? ""),
    queryFn: () => notificationsApi.preferences(getBrowserHttp(), userId!),
    enabled: userId !== null,
    staleTime: 60_000,
  });
}

export function useSaveNotificationPreferences() {
  const queryClient = useQueryClient();
  const userId = useUserId();
  return useMutation<NotificationPreferences, ApiError, NotificationPreferences>({
    mutationFn: (body) => notificationsApi.savePreferences(getBrowserHttp(), userId!, body),
    onSuccess: (saved) => queryClient.setQueryData(queryKeys.notifications.preferences(userId ?? ""), saved),
  });
}

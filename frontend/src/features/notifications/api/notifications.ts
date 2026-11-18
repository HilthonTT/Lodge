import type { Http } from "@/lib/api/client";
import type { AppNotification, CountResponse, NotificationPreferences, PagedList } from "@/types/api";

export type NotificationsParams = { page: number; pageSize: number; unreadOnly: boolean };

export const notificationsApi = {
  list: (http: Http, params: NotificationsParams) =>
    http.get<PagedList<AppNotification>>("/notifications", { params }).then((r) => r.data),

  unreadCount: (http: Http) => http.get<CountResponse>("/notifications/unread-count").then((r) => r.data.count),

  markRead: (http: Http, id: string) =>
    http.put<void>(`/notifications/${encodeURIComponent(id)}/read`).then(() => undefined),

  markAllRead: (http: Http) => http.put<void>("/notifications/read-all").then(() => undefined),

  preferences: (http: Http, userId: string) =>
    http
      .get<NotificationPreferences>(`/users/${encodeURIComponent(userId)}/notification-preferences`)
      .then((r) => r.data),

  savePreferences: (http: Http, userId: string, body: NotificationPreferences) =>
    http
      .put<NotificationPreferences>(`/users/${encodeURIComponent(userId)}/notification-preferences`, body)
      .then((r) => r.data),
};

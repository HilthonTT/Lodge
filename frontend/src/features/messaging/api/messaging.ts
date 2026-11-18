import type { Http } from "@/lib/api/client";
import type {
  Conversation,
  CountResponse,
  Message,
  MessagesPage,
  PagedList,
  StartConversationResponse,
} from "@/types/api";

const conversationPath = (id: string) => `/conversations/${encodeURIComponent(id)}`;

/** Longest message the API accepts (`Message.TooLong`). */
export const MESSAGE_MAX_LENGTH = 2000;

export const messagingApi = {
  /** Starts (or reuses) the conversation for a listing and sends the first message. */
  start: (http: Http, apartmentId: string, message: string) =>
    http.post<StartConversationResponse>("/conversations", { apartmentId, message }).then((r) => r.data),

  list: (http: Http, params: { page: number; pageSize: number }) =>
    http.get<PagedList<Conversation>>("/conversations", { params }).then((r) => r.data),

  get: (http: Http, id: string) => http.get<Conversation>(conversationPath(id)).then((r) => r.data),

  /** Newest page first; pass `nextBefore` as `before` to load older messages. */
  messages: (http: Http, id: string, params: { before?: string; limit?: number } = {}) =>
    http
      .get<MessagesPage>(`${conversationPath(id)}/messages`, {
        params: { limit: params.limit ?? 30, ...(params.before ? { before: params.before } : {}) },
      })
      .then((r) => r.data),

  send: (http: Http, id: string, body: string) =>
    http.post<Message>(`${conversationPath(id)}/messages`, { body }).then((r) => r.data),

  markRead: (http: Http, id: string) => http.put<void>(`${conversationPath(id)}/read`).then(() => undefined),

  unreadCount: (http: Http) =>
    http.get<CountResponse>("/conversations/unread-count").then((r) => r.data.count),
};

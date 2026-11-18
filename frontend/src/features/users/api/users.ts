import type { Http } from "@/lib/api/client";
import type { UpdateUserRequest, User } from "@/types/api";

export const usersApi = {
  me: (http: Http) => http.get<User>("/users/auth").then((r) => r.data),

  update: (http: Http, userId: string, body: UpdateUserRequest) =>
    http.put<void>(`/users/${encodeURIComponent(userId)}`, body).then(() => undefined),

  changePassword: (http: Http, userId: string, password: string) =>
    http
      .put<void>(`/users/${encodeURIComponent(userId)}/change-password`, { password })
      .then(() => undefined),
};

import type { Http } from "@/lib/api/client";
import type {
  AvatarResponse,
  DisableTwoFactorRequest,
  PublicProfile,
  RecoveryCodesResponse,
  TwoFactorSetupResponse,
  UpdateUserRequest,
  User,
} from "@/types/api";

const userPath = (userId: string) => `/users/${encodeURIComponent(userId)}`;

export const usersApi = {
  me: (http: Http) => http.get<User>("/users/auth").then((r) => r.data),

  /** Anonymous public profile. */
  profile: (http: Http, userId: string) =>
    http.get<PublicProfile>(`${userPath(userId)}/profile`).then((r) => r.data),

  update: (http: Http, userId: string, body: UpdateUserRequest) =>
    http.put<void>(userPath(userId), body).then(() => undefined),

  changePassword: (http: Http, userId: string, password: string) =>
    http.put<void>(`${userPath(userId)}/change-password`, { password }).then(() => undefined),

  uploadAvatar: (http: Http, userId: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return http.post<AvatarResponse>(`${userPath(userId)}/avatar`, form).then((r) => r.data);
  },

  deleteAvatar: (http: Http, userId: string) =>
    http.delete<void>(`${userPath(userId)}/avatar`).then(() => undefined),

  setupTwoFactor: (http: Http, userId: string) =>
    http.post<TwoFactorSetupResponse>(`${userPath(userId)}/two-factor/setup`).then((r) => r.data),

  enableTwoFactor: (http: Http, userId: string, code: string) =>
    http.post<RecoveryCodesResponse>(`${userPath(userId)}/two-factor/enable`, { code }).then((r) => r.data),

  disableTwoFactor: (http: Http, userId: string, body: DisableTwoFactorRequest) =>
    http.post<void>(`${userPath(userId)}/two-factor/disable`, body).then(() => undefined),

  regenerateRecoveryCodes: (http: Http, userId: string, code: string) =>
    http
      .post<RecoveryCodesResponse>(`${userPath(userId)}/two-factor/recovery-codes`, { code })
      .then((r) => r.data),
};

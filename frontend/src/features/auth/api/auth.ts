import type { Http } from "@/lib/api/client";
import { API_URL } from "@/lib/env";
import type {
  ExternalProvider,
  ExternalProviderName,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ResetPasswordRequest,
  TokenResponse,
  TwoFactorLoginRequest,
} from "@/types/api";

export const authApi = {
  login: (http: Http, body: LoginRequest) =>
    http.post<LoginResponse>("/authentication/login", body).then((r) => r.data),

  loginTwoFactor: (http: Http, body: TwoFactorLoginRequest) =>
    http.post<TokenResponse>("/authentication/login/two-factor", body).then((r) => r.data),

  register: (http: Http, body: RegisterRequest) =>
    http.post<TokenResponse>("/authentication/register", body).then((r) => r.data),

  verifyEmail: (http: Http, token: string) =>
    http.post<void>("/authentication/verify-email", { token }).then(() => undefined),

  resendVerification: (http: Http) =>
    http.post<void>("/authentication/resend-verification").then(() => undefined),

  forgotPassword: (http: Http, email: string) =>
    http.post<void>("/authentication/forgot-password", { email }).then(() => undefined),

  resetPassword: (http: Http, body: ResetPasswordRequest) =>
    http.post<void>("/authentication/reset-password", body).then(() => undefined),

  /** A fresh token with up-to-date claims (e-mail verified, avatar, name…). */
  refreshToken: (http: Http) =>
    http.post<TokenResponse>("/authentication/refresh-token").then((r) => r.data),

  providers: (http: Http) =>
    http.get<ExternalProvider[]>("/authentication/providers").then((r) => r.data),
};

/** Start of the social login flow: a full-page navigation, never an XHR. */
export function externalLoginUrl(provider: ExternalProviderName, returnUrl: string) {
  return `${API_URL}/authentication/external/${encodeURIComponent(provider)}?returnUrl=${encodeURIComponent(returnUrl)}`;
}

import type { Http } from "@/lib/api/client";
import type { LoginRequest, RegisterRequest, TokenResponse } from "@/types/api";

export const authApi = {
  login: (http: Http, body: LoginRequest) =>
    http.post<TokenResponse>("/authentication/login", body).then((r) => r.data),

  register: (http: Http, body: RegisterRequest) =>
    http.post<TokenResponse>("/authentication/register", body).then((r) => r.data),
};

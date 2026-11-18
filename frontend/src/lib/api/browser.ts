"use client";

import { readToken } from "@/lib/auth/token.client";
import { API_URL } from "@/lib/env";

import { createHttpClient, toApiError, type Http } from "./client";

export const UNAUTHORIZED_EVENT = "lodge:unauthorized";

let instance: Http | null = null;

/** Browser HTTP client: attaches the JWT from the cookie and broadcasts 401s. */
export function getBrowserHttp(): Http {
  if (instance) return instance;

  instance = createHttpClient(API_URL);
  instance.interceptors.request.use((config) => {
    const token = readToken();
    if (token) config.headers.set("Authorization", `Bearer ${token}`);
    return config;
  });
  instance.interceptors.response.use(undefined, (error: unknown) => {
    const apiError = toApiError(error);
    if (apiError.isUnauthorized && typeof window !== "undefined") {
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT));
    }
    return Promise.reject(apiError);
  });
  return instance;
}

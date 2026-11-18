import axios, { isAxiosError, type AxiosInstance } from "axios";

import type { ProblemDetails } from "@/types/api";

/** Normalized error thrown by every API call. */
export class ApiError extends Error {
  readonly status: number | undefined;
  readonly code: string | undefined;
  readonly details: string[];

  constructor(message: string, opts: { status?: number; code?: string; details?: string[] } = {}) {
    super(message);
    this.name = "ApiError";
    this.status = opts.status;
    this.code = opts.code;
    this.details = opts.details ?? [];
  }

  get isNetworkError() {
    return this.status === undefined;
  }
  get isUnauthorized() {
    return this.status === 401;
  }
  get isNotFound() {
    return this.status === 404;
  }
}

const FALLBACK_MESSAGES: Record<number, string> = {
  400: "The request was invalid.",
  401: "Your session has expired. Please sign in again.",
  403: "You don't have permission to do that.",
  404: "We couldn't find what you were looking for.",
  409: "That conflicts with an existing booking or record.",
  429: "Too many requests. Please try again in a moment.",
};

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (isAxiosError(error)) {
    const status = error.response?.status;
    if (!error.response) {
      return new ApiError("We can't reach the Lodge service right now. Please try again shortly.");
    }
    const data = error.response.data as ProblemDetails | string | undefined;
    const problem = typeof data === "object" && data !== null ? data : undefined;
    const details = problem?.errors?.map((e) => e.description).filter(Boolean) ?? [];
    const message =
      details[0] ||
      problem?.detail ||
      (status ? FALLBACK_MESSAGES[status] : undefined) ||
      "Something went wrong. Please try again.";
    return new ApiError(message, { status, code: problem?.title, details });
  }

  return new ApiError(error instanceof Error ? error.message : "Something went wrong.");
}

export function createHttpClient(baseURL: string, token?: string | null): AxiosInstance {
  const instance = axios.create({
    baseURL,
    timeout: 15_000,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  instance.interceptors.response.use(undefined, (error: unknown) => Promise.reject(toApiError(error)));

  return instance;
}

export type Http = AxiosInstance;

/** Runs an API call and returns a discriminated result instead of throwing. */
export async function attempt<T>(
  fn: () => Promise<T>,
): Promise<{ ok: true; data: T } | { ok: false; error: ApiError }> {
  try {
    return { ok: true, data: await fn() };
  } catch (error) {
    return { ok: false, error: toApiError(error) };
  }
}

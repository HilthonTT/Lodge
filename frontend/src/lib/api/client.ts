import axios, { isAxiosError, type AxiosInstance } from "axios";

import type { ProblemDetails } from "@/types/api";

/**
 * Frontend-owned fallback messages, translated under `errors.api.*` in
 * `messages/*.json` (see `useApiErrorMessage`). Messages that come from the
 * server are shown exactly as returned.
 */
export type ApiErrorMessageKey =
  | "badRequest"
  | "unauthorized"
  | "forbidden"
  | "notFound"
  | "conflict"
  | "tooManyRequests"
  | "unreachable"
  | "generic"
  | "signInAgain"
  | "invalidCheckoutLink";

/** Normalized error thrown by every API call. */
export class ApiError extends Error {
  readonly status: number | undefined;
  readonly code: string | undefined;
  readonly details: string[];
  /** Rule codes from a validation problem (`errors[].code`), e.g. "User.BioTooLong". */
  readonly codes: string[];
  /** Set when `message` is the frontend's own (English) fallback rather than the server's text. */
  readonly messageKey: ApiErrorMessageKey | undefined;

  constructor(
    message: string,
    opts: {
      status?: number;
      code?: string;
      details?: string[];
      codes?: string[];
      messageKey?: ApiErrorMessageKey;
    } = {},
  ) {
    super(message);
    this.name = "ApiError";
    this.status = opts.status;
    this.code = opts.code;
    this.details = opts.details ?? [];
    this.codes = opts.codes ?? [];
    this.messageKey = opts.messageKey;
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

const FALLBACK_MESSAGES: Record<number, [ApiErrorMessageKey, string]> = {
  400: ["badRequest", "The request was invalid."],
  401: ["unauthorized", "Your session has expired. Please sign in again."],
  403: ["forbidden", "You don't have permission to do that."],
  404: ["notFound", "We couldn't find what you were looking for."],
  409: ["conflict", "That conflicts with an existing booking or record."],
  429: ["tooManyRequests", "Too many requests. Please try again in a moment."],
};

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (isAxiosError(error)) {
    const status = error.response?.status;
    if (!error.response) {
      return new ApiError("We can't reach the Lodge service right now. Please try again shortly.", {
        messageKey: "unreachable",
      });
    }
    const data = error.response.data as ProblemDetails | string | undefined;
    const problem = typeof data === "object" && data !== null ? data : undefined;
    const details = problem?.errors?.map((e) => e.description).filter(Boolean) ?? [];
    const codes = problem?.errors?.map((e) => e.code).filter(Boolean) ?? [];
    const serverMessage = details[0] || problem?.detail;
    if (serverMessage) return new ApiError(serverMessage, { status, code: problem?.title, details, codes });

    const [messageKey, message] = (status ? FALLBACK_MESSAGES[status] : undefined) ?? [
      "generic",
      "Something went wrong. Please try again.",
    ];
    return new ApiError(message, { status, code: problem?.title, details, codes, messageKey });
  }

  return new ApiError("Something went wrong. Please try again.", { messageKey: "generic" });
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

"use client";

import { useQuery } from "@tanstack/react-query";

import { useSession } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { HostingSummary } from "@/types/api";

import { hostingApi } from "../api/hosting";

/** Host dashboard numbers. 403 `User.EmailNotVerified` for unverified accounts. */
export function useHostingSummary(options: { enabled?: boolean } = {}) {
  const { session } = useSession();
  return useQuery<HostingSummary, ApiError>({
    queryKey: queryKeys.hosting.summary,
    queryFn: () => hostingApi.summary(getBrowserHttp()),
    enabled: Boolean(session?.emailVerified) && (options.enabled ?? true),
    staleTime: 30_000,
    retry: (count, error) => error.status !== 403 && count < 2,
  });
}

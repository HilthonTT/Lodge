"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useCallback } from "react";

import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { LoginRequest, LoginResponse, RegisterRequest, ResetPasswordRequest, TwoFactorLoginRequest } from "@/types/api";

import { authApi } from "../api/auth";
import { useSession } from "../session-provider";

/** Signs in right away, or resolves with `requiresTwoFactor` for the second step. */
export function useLogin() {
  const { signIn } = useSession();
  return useMutation<LoginResponse, ApiError, LoginRequest>({
    mutationFn: async (body) => {
      const result = await authApi.login(getBrowserHttp(), body);
      if (result.token) signIn(result.token);
      return result;
    },
  });
}

export function useLoginTwoFactor() {
  const { signIn } = useSession();
  return useMutation<unknown, ApiError, TwoFactorLoginRequest>({
    mutationFn: async (body) => signIn((await authApi.loginTwoFactor(getBrowserHttp(), body)).token),
  });
}

export function useRegister() {
  const { signIn } = useSession();
  return useMutation<unknown, ApiError, RegisterRequest>({
    mutationFn: async (body) => signIn((await authApi.register(getBrowserHttp(), body)).token),
  });
}

export function useForgotPassword() {
  return useMutation<void, ApiError, string>({
    mutationFn: (email) => authApi.forgotPassword(getBrowserHttp(), email),
  });
}

export function useResetPassword() {
  return useMutation<void, ApiError, ResetPasswordRequest>({
    mutationFn: (body) => authApi.resetPassword(getBrowserHttp(), body),
  });
}

export function useVerifyEmail() {
  return useMutation<void, ApiError, string>({
    mutationFn: (token) => authApi.verifyEmail(getBrowserHttp(), token),
  });
}

export function useResendVerification() {
  return useMutation<void, ApiError, void>({
    mutationFn: () => authApi.resendVerification(getBrowserHttp()),
  });
}

/**
 * Swaps the session cookie for a fresh token so claims such as `emailVerified`,
 * the name and the avatar are current, then re-renders server components.
 * Resolves false (and keeps the old session) when the refresh fails.
 */
export function useRefreshSession() {
  const { signIn } = useSession();
  const router = useRouter();
  return useCallback(async () => {
    try {
      const { token } = await authApi.refreshToken(getBrowserHttp());
      signIn(token);
      router.refresh();
      return true;
    } catch {
      return false;
    }
  }, [router, signIn]);
}

/** Social login providers configured on the server (usually none locally). */
export function useExternalProviders() {
  return useQuery({
    queryKey: queryKeys.auth.providers,
    queryFn: () => authApi.providers(getBrowserHttp()),
    staleTime: 5 * 60_000,
  });
}

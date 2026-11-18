"use client";

import { useMutation } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import type { LoginRequest, RegisterRequest } from "@/types/api";

import { authApi } from "../api/auth";
import { useSession } from "../session-provider";

export function useLogin() {
  const { signIn } = useSession();
  return useMutation<unknown, ApiError, LoginRequest>({
    mutationFn: async (body) => signIn((await authApi.login(getBrowserHttp(), body)).token),
  });
}

export function useRegister() {
  const { signIn } = useSession();
  return useMutation<unknown, ApiError, RegisterRequest>({
    mutationFn: async (body) => signIn((await authApi.register(getBrowserHttp(), body)).token),
  });
}

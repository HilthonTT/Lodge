"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useRefreshSession } from "@/features/auth/hooks/use-auth-mutations";
import { useSession } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { DisableTwoFactorRequest, RecoveryCodesResponse, TwoFactorSetupResponse, User } from "@/types/api";

import { usersApi } from "../api/users";

function requireUser(userId: string | undefined): string {
  if (!userId) throw new ApiError("Please sign in again to continue.", { status: 401, messageKey: "signInAgain" });
  return userId;
}

/** The signed-in user's profile (GET /users/auth). */
export function useCurrentUser() {
  const { session } = useSession();
  return useQuery({
    queryKey: queryKeys.users.me,
    queryFn: () => usersApi.me(getBrowserHttp()),
    enabled: session !== null,
  });
}

/** After a profile change: reload the user and swap the token, so the navbar shows the new name/avatar. */
function useAfterProfileChange() {
  const queryClient = useQueryClient();
  const refreshSession = useRefreshSession();
  return async (userId: string) => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.users.me });
    await queryClient.invalidateQueries({ queryKey: queryKeys.users.profile(userId) });
    await refreshSession();
  };
}

type ProfilePatch = { user: User } & Partial<Pick<User, "firstName" | "lastName" | "bio">>;

/** PUT /users/{id}. Sends the current name and imageId back; `bio: null` leaves the bio unchanged. */
export function useUpdateProfile() {
  const queryClient = useQueryClient();
  const { updateSession } = useSession();
  const afterChange = useAfterProfileChange();
  return useMutation<void, ApiError, ProfilePatch>({
    mutationFn: ({ user, firstName = user.firstName, lastName = user.lastName, bio = null }) =>
      usersApi.update(getBrowserHttp(), user.id, { firstName, lastName, imageId: user.imageId, bio }),
    onSuccess: async (_, { user, firstName = user.firstName, lastName = user.lastName, bio }) => {
      queryClient.setQueryData<User>(queryKeys.users.me, {
        ...user,
        firstName,
        lastName,
        bio: bio === undefined ? user.bio : bio || null,
      });
      updateSession({ name: `${firstName} ${lastName}` });
      await afterChange(user.id);
    },
  });
}

export function useUploadAvatar() {
  const { session } = useSession();
  const afterChange = useAfterProfileChange();
  return useMutation<string, ApiError, File>({
    mutationFn: async (file) =>
      (await usersApi.uploadAvatar(getBrowserHttp(), requireUser(session?.userId), file)).avatarUrl,
    onSuccess: () => afterChange(requireUser(session?.userId)),
  });
}

export function useDeleteAvatar() {
  const { session } = useSession();
  const afterChange = useAfterProfileChange();
  return useMutation<void, ApiError, void>({
    mutationFn: () => usersApi.deleteAvatar(getBrowserHttp(), requireUser(session?.userId)),
    onSuccess: () => afterChange(requireUser(session?.userId)),
  });
}

export function useChangePassword() {
  const { session } = useSession();
  return useMutation<void, ApiError, string>({
    mutationFn: (password) => usersApi.changePassword(getBrowserHttp(), requireUser(session?.userId), password),
  });
}

export function useSetupTwoFactor() {
  const { session } = useSession();
  return useMutation<TwoFactorSetupResponse, ApiError, void>({
    mutationFn: () => usersApi.setupTwoFactor(getBrowserHttp(), requireUser(session?.userId)),
  });
}

/** Enabling, disabling and new recovery codes all change `twoFactorEnabled`, so refresh the user and token. */
function useTwoFactorChanged() {
  const queryClient = useQueryClient();
  const refreshSession = useRefreshSession();
  return async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.users.me });
    await refreshSession();
  };
}

export function useEnableTwoFactor() {
  const { session } = useSession();
  const changed = useTwoFactorChanged();
  return useMutation<RecoveryCodesResponse, ApiError, string>({
    mutationFn: (code) => usersApi.enableTwoFactor(getBrowserHttp(), requireUser(session?.userId), code),
    onSuccess: changed,
  });
}

export function useDisableTwoFactor() {
  const { session } = useSession();
  const changed = useTwoFactorChanged();
  return useMutation<void, ApiError, DisableTwoFactorRequest>({
    mutationFn: (body) => usersApi.disableTwoFactor(getBrowserHttp(), requireUser(session?.userId), body),
    onSuccess: changed,
  });
}

export function useRegenerateRecoveryCodes() {
  const { session } = useSession();
  return useMutation<RecoveryCodesResponse, ApiError, string>({
    mutationFn: (code) => usersApi.regenerateRecoveryCodes(getBrowserHttp(), requireUser(session?.userId), code),
  });
}

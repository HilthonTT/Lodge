"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { User } from "@/types/api";

import { usersApi } from "../api/users";

/** The signed-in user's profile (GET /users/auth). */
export function useCurrentUser() {
  const { session } = useSession();
  return useQuery({
    queryKey: queryKeys.users.me,
    queryFn: () => usersApi.me(getBrowserHttp()),
    enabled: session !== null,
  });
}

export function useUpdateName() {
  const queryClient = useQueryClient();
  const { updateSession } = useSession();
  return useMutation<void, ApiError, { user: User; firstName: string; lastName: string }>({
    // Send the existing imageId back: omitting it would clear the user's picture.
    mutationFn: ({ user, firstName, lastName }) =>
      usersApi.update(getBrowserHttp(), user.id, { firstName, lastName, imageId: user.imageId }),
    onSuccess: (_, { user, firstName, lastName }) => {
      queryClient.setQueryData<User>(queryKeys.users.me, { ...user, firstName, lastName });
      updateSession({ name: `${firstName} ${lastName}` });
    },
  });
}

export function useChangePassword() {
  const { session } = useSession();
  return useMutation<void, ApiError, string>({
    mutationFn: (password) => {
      if (!session) throw new ApiError("Please sign in again to continue.", { status: 401 });
      return usersApi.changePassword(getBrowserHttp(), session.userId, password);
    },
  });
}

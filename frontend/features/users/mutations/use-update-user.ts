import { useMutation } from "@tanstack/react-query";

import { updateUser } from "@/actions/users/update-user";
import { clearToken } from "@/lib/auth";

export const useUpdateUser = () => {
  const mutation = useMutation({
    mutationFn: async (request: UpdateUserRequest) => await updateUser(request),
    onSuccess: () => {
      clearToken();
    },
  });

  return mutation;
};

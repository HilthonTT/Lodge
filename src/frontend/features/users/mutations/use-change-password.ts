import { changePassword } from "@/actions/users/change-password";
import { useMutation } from "@tanstack/react-query";

export const useChangePassword = () => {
  const mutation = useMutation({
    mutationFn: async (request: ChangePasswordRequest) =>
      await changePassword(request),
  });

  return mutation;
};

import { useMutation } from "@tanstack/react-query";

import { register } from "@/actions/authentication/register";
import { storeToken } from "@/lib/auth";

export const useRegister = () => {
  const mutation = useMutation({
    mutationFn: (request: RegisterRequest) => register(request),
    onSuccess: (response) => {
      if (response) {
        storeToken(response.token);
      }
    },
  });

  return mutation;
};

import { useMutation } from "@tanstack/react-query";

import { login } from "@/actions/authentication/login";
import { storeToken } from "@/lib/auth";

export const useLogin = () => {
  const mutation = useMutation({
    mutationFn: (request: LoginRequest) => login(request),
    onSuccess: (response) => {
      if (response) {
        storeToken(response.token);
      }
    },
  });

  return mutation;
};

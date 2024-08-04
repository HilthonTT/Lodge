import { useMutation } from "@tanstack/react-query";

import { login } from "@/features/authentication/api/login";

export const useLogin = () => {
  const mutation = useMutation({
    mutationFn: (request: LoginRequest) => login(request),
  });

  return mutation;
};

import { useMutation } from "@tanstack/react-query";

import { register } from "@/features/authentication/api/register";

export const useRegister = () => {
  const mutation = useMutation({
    mutationFn: (request: RegisterRequest) => register(request),
  });

  return mutation;
};

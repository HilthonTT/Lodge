import { storeToken } from "@/lib/auth";
import { requestPostAxios } from "@/lib/axios";

export const login = async (request: LoginRequest) => {
  const response = (await requestPostAxios(
    "authentication/login",
    request
  )) as TokenResponse;

  storeToken(response.token);

  return response.token;
};

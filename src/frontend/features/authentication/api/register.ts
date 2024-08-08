import { storeToken } from "@/lib/auth";
import { requestPostAxios } from "@/lib/axios";

export const register = async (request: RegisterRequest) => {
  const response = (await requestPostAxios(
    "authentication/register",
    request
  )) as TokenResponse;

  storeToken(response.token);

  return response.token;
};

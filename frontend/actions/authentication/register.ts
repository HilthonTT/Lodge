"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const register = async (request: RegisterRequest) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.post(
      `/api/${API_VERSION}/authentication/register`,
      request
    );

    return response.data as TokenResponse;
  } catch (error) {
    console.log("[AUTH_REGISTER_FAILED]", error);
    return null;
  }
};

"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const login = async (request: LoginRequest) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.post(
      `/api/${API_VERSION}/authentication/login`,
      request
    );

    return response.data as TokenResponse;
  } catch (error) {
    console.log("[AUTH_LOGIN_FAILED]", error);
    return null;
  }
};

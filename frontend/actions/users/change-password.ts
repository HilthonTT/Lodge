"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const changePassword = async (request: ChangePasswordRequest) => {
  try {
    const axios = createAxiosInstance();

    const { jwtToken, userId, ...requestBody } = request;

    await axios.put(
      `/api/${API_VERSION}/users/${userId}/change-password`,
      requestBody,
      {
        headers: {
          Authorization: `Bearer ${jwtToken}`,
        },
      }
    );
  } catch (error) {
    console.log("[CHANGE_PASSWORD]", error);
    throw error;
  }
};

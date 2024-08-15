"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const updateUser = async (request: UpdateUserRequest) => {
  try {
    const axios = createAxiosInstance();

    const { jwtToken, userId, ...requestBody } = request;

    await axios.put(`/api/${API_VERSION}/users/${userId}`, requestBody, {
      headers: {
        Authorization: `Bearer ${jwtToken}`,
      },
    });
  } catch (error) {
    console.log("[UPDATE_USER]", error);
    throw error;
  }
};

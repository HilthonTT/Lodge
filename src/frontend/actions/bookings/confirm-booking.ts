"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const confirmBooking = async (request: ConfirmBookingRequest) => {
  try {
    const axios = createAxiosInstance();

    const { jwtToken, ...requestBody } = request;

    const response = await axios.post(
      `/api/${API_VERSION}/stripe/checkout`,
      requestBody,
      {
        headers: {
          Authorization: `Bearer ${jwtToken}`,
        },
      }
    );

    const url = response.data as string;

    return url;
  } catch (error) {
    console.log("[CONFIRM_BOOKING]", error);
    return "";
  }
};

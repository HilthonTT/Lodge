"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const cancelBooking = async (request: CancelBookingRequest) => {
  try {
    const axios = createAxiosInstance();

    const { jwtToken, bookingId, ...requestBody } = request;

    await axios.put(
      `/api/${API_VERSION}/bookings/${bookingId}/cancel`,
      requestBody,
      {
        headers: {
          Authorization: `Bearer ${jwtToken}`,
        },
      }
    );
  } catch (error) {
    console.log("[CANCEL_BOOKING]", error);
    throw error;
  }
};

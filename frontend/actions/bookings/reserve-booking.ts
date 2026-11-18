"use server";

import { v4 as uuid } from "uuid";

import { createAxiosInstance } from "@/lib/axios";
import { API_VERSION } from "@/constants";

export const reserveBooking = async (request: ReserveBookingRequest) => {
  try {
    const { jwtToken, ...requestBody } = request;
    const idempotentId = uuid();

    const axios = createAxiosInstance();

    const response = await axios.post(
      `/api/${API_VERSION}/bookings`,
      requestBody,
      {
        headers: {
          "X-Idempotency-Key": idempotentId,
          Authorization: `Bearer ${jwtToken}`,
        },
      }
    );

    return response.data as string;
  } catch (error) {
    console.log("[RESERVE_BOOKING]", error);
    throw error;
  }
};

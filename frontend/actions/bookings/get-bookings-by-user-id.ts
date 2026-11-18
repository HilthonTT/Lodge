"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getBookingsByUserId = async (userId: string, jwtToken: string) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.get(
      `/api/${API_VERSION}/users/${userId}/bookings`,
      {
        headers: {
          Authorization: `Bearer ${jwtToken}`,
        },
      }
    );

    return response.data as Booking[];
  } catch (error) {
    console.log("[GET_BOOKINGS_BY_USER_ID]", error);
    return [];
  }
};

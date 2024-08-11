"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getApartmentBookedDates = async (apartmentId: string) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.get(
      `/api/${API_VERSION}/apartments/${apartmentId}/booked-dates`
    );

    return response.data as Date[];
  } catch (error) {
    console.log("[GET_APARTMENT_BOOKED_DATES]", error);
    return [];
  }
};

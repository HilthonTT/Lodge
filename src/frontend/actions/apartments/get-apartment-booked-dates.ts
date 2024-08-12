"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getApartmentBookedDates = async (apartmentId: string) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.get(
      `/api/${API_VERSION}/apartments/${apartmentId}/booked-dates`
    );

    // Convert the dates to Date objects and filter out dates before today
    const bookedDates = response.data as Date[];
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Reset time to 00:00:00 for accurate comparison

    const filteredDates = bookedDates.filter((date) => {
      const bookedDate = new Date(date);
      return bookedDate >= today;
    });

    return filteredDates;
  } catch (error) {
    console.log("[GET_APARTMENT_BOOKED_DATES]", error);
    return [];
  }
};

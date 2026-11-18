"use server";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getApartmentById = async (apartmentId: string) => {
  try {
    const axios = createAxiosInstance();

    const response = await axios.get(
      `api/${API_VERSION}/apartments/${apartmentId}`
    );

    return response.data as Apartment;
  } catch (error) {
    console.log("[GET_APARTMENT_BY_ID]", error);
    return null;
  }
};

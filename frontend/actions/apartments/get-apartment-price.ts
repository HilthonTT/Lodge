"use server";

import qs from "query-string";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getApartmentPrice = async (
  apartmentId: string,
  startDate: string,
  endDate: string
) => {
  try {
    const axios = createAxiosInstance();

    console.log(startDate, endDate);

    const url = qs.stringifyUrl(
      {
        url: `api/${API_VERSION}/apartments/${apartmentId}/price`,
        query: {
          startDate,
          endDate,
        },
      },
      {
        skipEmptyString: true,
        skipNull: true,
      }
    );

    const response = await axios.get(url);

    return response.data as PriceDetails;
  } catch (error) {
    console.log("[GET_APARTMENT_PRICE]", error);
    return null;
  }
};

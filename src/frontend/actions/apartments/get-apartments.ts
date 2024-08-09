"use server";

import qs from "query-string";

import { API_VERSION } from "@/constants";
import { createAxiosInstance } from "@/lib/axios";

export const getApartments = async (page: number, pageSize: number) => {
  try {
    const axios = createAxiosInstance();

    const url = qs.stringifyUrl({
      url: `/api/${API_VERSION}/apartments`,
      query: {
        page,
        pageSize,
      },
    });

    const response = await axios.get(url);

    return response.data as ApartmentPagedList;
  } catch (error) {
    console.log("[GET_APARTMENTS]", error);
  }
};

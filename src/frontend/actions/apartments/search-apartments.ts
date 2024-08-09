"use server";

import qs from "query-string";

import { createAxiosInstance } from "@/lib/axios";
import { API_VERSION } from "@/constants";

type Props = {
  page: number;
  pageSize: number;
  searchTerm?: string;
  sortColumn?: "name" | "description" | "country" | "price_amount" | "currency";
  sortOrder?: "asc" | "desc";
  startDate?: string;
  endDate?: string;
  guestCount?: number;
  roomCount?: number;
};

export const searchApartments = async (query: Props) => {
  try {
    const axios = createAxiosInstance();

    const url = qs.stringifyUrl(
      {
        url: `/api/${API_VERSION}/apartments/search`,
        query,
      },
      {
        skipEmptyString: true,
        skipNull: true,
      }
    );

    const response = await axios.get(url);

    return {
      isSuccess: true,
      data: response.data as ApartmentPagedList,
    };
  } catch (error) {
    console.log("[SEARCH_APARTMENTS]", error);
    return { isSuccess: false };
  }
};

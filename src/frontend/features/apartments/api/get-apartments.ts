import qs from "query-string";

import { API_VERSION, BASE_API_URL } from "@/constants";
import { Method } from "@/enums";

export const getApartments = async (page: number, pageSize: number = 36) => {
  const url = qs.stringifyUrl(
    {
      url: `${BASE_API_URL}/api/${API_VERSION}/apartments`,
      query: {
        page,
        pageSize,
      },
    },
    {
      skipEmptyString: true,
      skipNull: true,
    }
  );

  const response = await fetch(url, {
    method: Method.GET,
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    throw new Error("Something went wrong while fetching the apartments.");
  }

  const apartments = (await response.json()) as ApartmentPagedList;

  return apartments;
};

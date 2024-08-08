import qs from "query-string";
import axios from "axios";

import { API_VERSION, BASE_API_URL } from "@/constants";

export const requestGetAxios = async (
  endpoint: string,
  query: qs.StringifiableRecord
) => {
  const url = qs.stringifyUrl(
    {
      url: `${BASE_API_URL}/api/${API_VERSION}/${endpoint}`,
      query,
    },
    {
      skipEmptyString: true,
      skipNull: true,
    }
  );

  try {
    const response = await axios.get(url, {
      headers: {
        "Content-Type": "application/json",
      },
    });

    return response.data;
  } catch (error) {
    throw new Error("Something went wrong while fetching.");
  }
};

export const requestPostAxios = async (endpoint: string, data: any) => {
  const url = `${BASE_API_URL}/api/${API_VERSION}/${endpoint}`;

  try {
    const response = await axios.post(url, data);

    return response.data;
  } catch (error) {
    throw new Error("Something went wrong while making the POST request.");
  }
};

export const performPutAxios = async (endpoint: string, data: any) => {
  const url = `${BASE_API_URL}/api/${API_VERSION}/${endpoint}`;

  try {
    const response = await axios.put(url, data, {
      headers: {
        "Content-Type": "application/json",
      },
    });

    return response.data;
  } catch (error) {
    throw new Error("Something went wrong while making the PUT request.");
  }
};

export const performDeleteAxios = async (endpoint: string) => {
  const url = `${BASE_API_URL}/api/${API_VERSION}/${endpoint}`;

  try {
    const response = await axios.delete(url, {
      headers: {
        "Content-Type": "application/json",
      },
    });

    return response.data;
  } catch (error) {
    throw new Error("Something went wrong while making the DELETE request.");
  }
};

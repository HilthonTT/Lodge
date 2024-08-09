import axios from "axios";
import https from "https";

import { BASE_API_URL } from "@/constants";

export const createAxiosInstance = () => {
  const instance = axios.create({
    headers: {
      "Content-Type": "application/json",
    },
    httpsAgent: new https.Agent({
      rejectUnauthorized: false,
    }),
    baseURL: BASE_API_URL,
  });

  return instance;
};

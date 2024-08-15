import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";

import { TOKEN_EXPIRATION_TIME } from "@/constants";

const TOKEN_KEY = "token";

export interface JwtPayload {
  sub: string;
  exp: number;
  name: string;
  email: string;
  userId: string;
  imageId?: string;
}

export const storeToken = (token: string) => {
  // Store token for 1 hour
  Cookies.set(TOKEN_KEY, token, { expires: TOKEN_EXPIRATION_TIME });
};

export const fetchToken = () => {
  return Cookies.get(TOKEN_KEY);
};

export const extractJwtPayload = (token: string) => {
  const decodedClaims = jwtDecode<JwtPayload>(token);

  return decodedClaims;
};

export const clearToken = () => {
  Cookies.remove(TOKEN_KEY);
};

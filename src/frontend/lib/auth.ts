import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";

import { TOKEN_EXPIRATION_TIME } from "@/constants";

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
  Cookies.set("token", token, { expires: TOKEN_EXPIRATION_TIME });
};

export const fetchToken = () => {
  return Cookies.get("token");
};

export const extractJwtPayload = (token: string) => {
  const decodedClaims = jwtDecode<JwtPayload>(token);

  return decodedClaims;
};

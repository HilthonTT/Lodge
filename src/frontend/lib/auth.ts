import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";

export interface JwtPayload {
  sub: string;
  exp: number;
  name: string;
  email: string;
  userId: string;
  imageId?: string;
}

export const storeToken = (token: string) => {
  Cookies.set("token", token, { expires: 1 }); // Store token for 1 day
};

export const fetchToken = () => {
  return Cookies.get("token");
};

export const extractJwtPayload = (token: string) => {
  const decodedClaims = jwtDecode<JwtPayload>(token);

  return decodedClaims;
};

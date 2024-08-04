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

export const login = async (email: string, password: string) => {
  const response = await fetch(
    "https://localhost:5001/api/v1/authentication/login",
    {
      method: "post",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    }
  );

  if (!response.ok) {
    throw new Error("Login failed.");
  }

  const data = await response.text();

  return data;
};

export const storeToken = (token: string) => {
  Cookies.set("token", token, { expires: 1 }); // Store token for 1 day
};

export const extractJwtPayload = (token: string) => {
  const decodedClaims = jwtDecode<JwtPayload>(token);

  return decodedClaims;
};

import { API_VERSION, BASE_API_URL } from "@/constants";
import { storeToken } from "@/lib/auth";
import { Methods } from "@/enums";

export const login = async ({ email, password }: LoginRequest) => {
  const response = await fetch(
    `${BASE_API_URL}/api/${API_VERSION}/authentication/login`,
    {
      method: Methods.POST,
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    }
  );

  if (!response.ok) {
    throw new Error("Something went wrong while logging in.");
  }

  const token = await response.text();

  storeToken(token);

  return token;
};

import { API_VERSION, BASE_API_URL } from "@/constants";
import { storeToken } from "@/lib/auth";
import { Method } from "@/enums";

export const login = async (request: LoginRequest) => {
  const response = await fetch(
    `${BASE_API_URL}/api/${API_VERSION}/authentication/login`,
    {
      method: Method.POST,
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    }
  );

  if (!response.ok) {
    throw new Error("Something went wrong while logging in.");
  }

  const token = await response.text();

  storeToken(token);

  return token;
};

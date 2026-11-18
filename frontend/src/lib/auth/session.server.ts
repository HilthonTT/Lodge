import "server-only";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { cache } from "react";

import { loginUrl } from "./routes";
import { decodeSession, isAdminSession, TOKEN_COOKIE, type Session } from "./token";

/** Raw token from the request cookie, or null when missing/expired. */
export const getServerToken = cache(async (): Promise<string | null> => {
  const token = (await cookies()).get(TOKEN_COOKIE)?.value;
  return decodeSession(token) ? (token ?? null) : null;
});

export const getServerSession = cache(async (): Promise<Session | null> => {
  return decodeSession(await getServerToken());
});

/**
 * Server-side guard for admin pages (the proxy check is only optimistic).
 * Signed-out users go to the login page, everyone else who isn't an admin to "/".
 * The API still authorizes every admin request on its own.
 */
export async function requireAdminSession(next: string): Promise<Session> {
  const session = await getServerSession();
  if (!session) redirect(loginUrl(next));
  if (!isAdminSession(session)) redirect("/");
  return session;
}

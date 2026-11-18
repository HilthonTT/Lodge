"use client";

import Cookies from "js-cookie";

import { decodeSession, TOKEN_COOKIE } from "./token";

export function readToken(): string | null {
  const token = Cookies.get(TOKEN_COOKIE) ?? null;
  // Never hand out an expired token; drop it so the proxy and server agree.
  if (token && !decodeSession(token)) {
    Cookies.remove(TOKEN_COOKIE, { path: "/" });
    return null;
  }
  return token;
}

/** Persists the token until the JWT itself expires. */
export function writeToken(token: string) {
  const session = decodeSession(token);
  if (!session) throw new Error("Received an invalid or expired token.");
  Cookies.set(TOKEN_COOKIE, token, {
    expires: new Date(session.expiresAt),
    path: "/",
    sameSite: "lax",
    secure: window.location.protocol === "https:",
  });
  return session;
}

export function removeToken() {
  Cookies.remove(TOKEN_COOKIE, { path: "/" });
}

import { jwtDecode } from "jwt-decode";

import type { UserRole } from "@/types/api";

export const TOKEN_COOKIE = "lodge_token";

/** Claims issued by Lodge.Infrastructure/Authentication/JwtProvider. */
type LodgeJwtClaims = {
  userId?: string;
  email?: string;
  name?: string;
  imageId?: string;
  /** "Guest" or "Admin". A string, or an array if several roles are issued. */
  role?: string | string[];
  /** Long-form claim name some .NET token handlers emit instead of `role`. */
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string | string[];
  exp?: number;
};

const ROLE_CLAIM_URI = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function readRole(claims: LodgeJwtClaims): UserRole {
  const raw = claims.role ?? claims[ROLE_CLAIM_URI];
  const roles = Array.isArray(raw) ? raw : raw ? [raw] : [];
  return roles.some((r) => r.toLowerCase() === "admin") ? "Admin" : "Guest";
}

export type Session = {
  userId: string;
  email: string;
  name: string;
  imageId: string | null;
  role: UserRole;
  /** Expiry as epoch milliseconds. */
  expiresAt: number;
};

/** Small skew so we never send a token that expires in flight. */
const EXPIRY_SKEW_MS = 10_000;

/** Decodes (does not verify) a token and returns a session if it is well-formed and unexpired. */
export function decodeSession(token: string | null | undefined, now = Date.now()): Session | null {
  if (!token) return null;
  try {
    const claims = jwtDecode<LodgeJwtClaims>(token);
    if (!claims.userId || !claims.exp) return null;
    const expiresAt = claims.exp * 1000;
    if (expiresAt - EXPIRY_SKEW_MS <= now) return null;
    return {
      userId: claims.userId,
      email: claims.email ?? "",
      name: claims.name?.trim() || claims.email || "Guest",
      imageId: claims.imageId || null,
      role: readRole(claims),
      expiresAt,
    };
  } catch {
    return null;
  }
}

/** Whether the (optimistically decoded) session belongs to an administrator. */
export function isAdminSession(session: Session | null | undefined): boolean {
  return session?.role === "Admin";
}

/** Only allow same-origin relative redirects ("/bookings"), never "//evil.com". */
export function safeRedirectPath(value: string | null | undefined, fallback = "/"): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) {
    return fallback;
  }
  return value;
}

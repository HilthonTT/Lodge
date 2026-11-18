import { NextResponse, type NextRequest } from "next/server";

import { isAdminPath, isGuestOnlyPath, isProtectedPath, loginUrl } from "@/lib/auth/routes";
import { decodeSession, isAdminSession, safeRedirectPath, TOKEN_COOKIE } from "@/lib/auth/token";

/**
 * Next 16 "proxy" (formerly middleware): optimistic auth gate based on the JWT
 * cookie. The API still verifies the signature on every request.
 */
export function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;
  const token = request.cookies.get(TOKEN_COOKIE)?.value;
  const session = decodeSession(token);

  if (isProtectedPath(pathname) && !session) {
    const response = NextResponse.redirect(new URL(loginUrl(pathname + search), request.url));
    if (token) response.cookies.delete(TOKEN_COOKIE);
    return response;
  }

  // Signed in but not an admin: send them home rather than to a 403 page.
  if (isAdminPath(pathname) && !isAdminSession(session)) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  if (isGuestOnlyPath(pathname) && session) {
    const next = safeRedirectPath(request.nextUrl.searchParams.get("next"));
    return NextResponse.redirect(new URL(next, request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/admin/:path*", "/bookings/:path*", "/settings/:path*", "/stays/:path*", "/login", "/register"],
};

/** Route prefixes that require a signed-in user (enforced in `src/proxy.ts`). */
export const PROTECTED_PREFIXES = [
  "/bookings",
  "/settings",
  "/stays",
  "/checkout",
  "/admin",
  "/hosting",
  "/wishlists",
  "/messages",
  "/notifications",
  "/experience-bookings",
] as const;

/** Route prefixes that additionally require the Admin role. */
export const ADMIN_PREFIXES = ["/admin"] as const;

/** Pages a signed-in user should not see. */
export const GUEST_ONLY_PATHS = ["/login", "/register"] as const;

export function isProtectedPath(pathname: string) {
  return PROTECTED_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`));
}

export function isAdminPath(pathname: string) {
  return ADMIN_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`));
}

export function isGuestOnlyPath(pathname: string) {
  return GUEST_ONLY_PATHS.some((p) => pathname === p);
}

export function loginUrl(next?: string) {
  return next && next !== "/" ? `/login?next=${encodeURIComponent(next)}` : "/login";
}

"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { createContext, use, useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";

import { UNAUTHORIZED_EVENT } from "@/lib/api/browser";
import { isProtectedPath, loginUrl } from "@/lib/auth/routes";
import { isAdminSession, type Session } from "@/lib/auth/token";
import { removeToken, writeToken } from "@/lib/auth/token.client";

type SignOutOptions = {
  /** Where to go afterwards. Defaults to the login page when on a protected route. */
  redirectTo?: string;
  reason?: "expired" | "user";
};

type SessionContextValue = {
  session: Session | null;
  isAuthenticated: boolean;
  /** Optimistic (decoded, not verified) role check; the API enforces it. */
  isAdmin: boolean;
  signIn: (token: string) => Session;
  signOut: (options?: SignOutOptions) => void;
  /** Patch local session claims (e.g. after a name change). */
  updateSession: (patch: Partial<Pick<Session, "name">>) => void;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({
  initialSession,
  children,
}: {
  initialSession: Session | null;
  children: React.ReactNode;
}) {
  const t = useTranslations("auth");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [session, setSession] = useState<Session | null>(initialSession);

  const signIn = useCallback((token: string) => {
    const next = writeToken(token);
    setSession(next);
    return next;
  }, []);

  const signOut = useCallback(
    ({ redirectTo, reason = "user" }: SignOutOptions = {}) => {
      removeToken();
      setSession(null);
      queryClient.removeQueries({ queryKey: ["bookings"] });
      queryClient.removeQueries({ queryKey: ["users"] });
      queryClient.removeQueries({ queryKey: ["hosting"] });
      queryClient.removeQueries({ queryKey: ["wishlists"] });
      queryClient.removeQueries({ queryKey: ["messaging"] });
      queryClient.removeQueries({ queryKey: ["notifications"] });
      queryClient.removeQueries({ queryKey: ["experience-bookings"] });

      if (reason === "expired") {
        toast.info(t("sessionExpired"), { description: t("sessionExpiredDescription") });
      }

      const path = window.location.pathname;
      const target =
        redirectTo ?? (isProtectedPath(path) ? loginUrl(path + window.location.search) : undefined);
      if (target) router.replace(target);
      router.refresh();
    },
    [queryClient, router, t],
  );

  const updateSession = useCallback((patch: Partial<Pick<Session, "name">>) => {
    setSession((current) => (current ? { ...current, ...patch } : current));
  }, []);

  // Sign out automatically when the JWT expires (the API issues 60 minute tokens).
  useEffect(() => {
    if (!session) return;
    const delay = session.expiresAt - Date.now();
    // setTimeout overflows above ~24.8 days; tokens are short-lived anyway.
    if (delay > 2_147_000_000) return;
    const timer = window.setTimeout(() => signOut({ reason: "expired" }), Math.max(delay, 0));
    return () => window.clearTimeout(timer);
  }, [session, signOut]);

  // Any 401 from the API means the token is no longer accepted.
  useEffect(() => {
    if (!session) return;
    const onUnauthorized = () => signOut({ reason: "expired" });
    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
  }, [session, signOut]);

  const value = useMemo<SessionContextValue>(
    () => ({
      session,
      isAuthenticated: session !== null,
      isAdmin: isAdminSession(session),
      signIn,
      signOut,
      updateSession,
    }),
    [session, signIn, signOut, updateSession],
  );

  return <SessionContext value={value}>{children}</SessionContext>;
}

export function useSession() {
  const context = use(SessionContext);
  if (!context) throw new Error("useSession must be used inside <SessionProvider>");
  return context;
}

/**
 * Current user id for protected screens. Null only briefly, e.g. right after the
 * session expired and before the redirect to /login happens.
 */
export function useUserId(): string | null {
  return useSession().session?.userId ?? null;
}

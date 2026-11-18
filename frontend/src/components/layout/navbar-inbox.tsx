"use client";

import { useSession } from "@/features/auth/session-provider";
import { MessagesNavLink } from "@/features/messaging/components/messages-nav-link";
import { NotificationBell } from "@/features/notifications/components/notification-bell";

/** Messages and notifications icons; signed-in users only. */
export function NavbarInbox() {
  const { isAuthenticated } = useSession();
  if (!isAuthenticated) return null;
  return (
    <>
      <MessagesNavLink />
      <NotificationBell />
    </>
  );
}

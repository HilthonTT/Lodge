"use client";

import { MessageCircle } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { CountBadge } from "@/features/notifications/components/notification-bell";
import { cn } from "@/lib/utils";

import { useUnreadMessageCount } from "../hooks/use-messaging";

/** Navbar link to the inbox with the unread message count. */
export function MessagesNavLink({ className }: { className?: string }) {
  const t = useTranslations("messaging");
  const pathname = usePathname();
  const unread = useUnreadMessageCount();
  const count = unread.data ?? 0;
  const active = pathname === "/messages" || pathname.startsWith("/messages/");

  return (
    <Button
      asChild
      variant="ghost"
      size="icon"
      className={cn("relative rounded-full", active && "bg-muted", className)}
    >
      <Link
        href="/messages"
        aria-label={count > 0 ? t("navUnread", { count }) : t("nav")}
        aria-current={active ? "page" : undefined}
        data-testid="messages-nav"
      >
        <MessageCircle aria-hidden="true" />
        <CountBadge count={count} />
      </Link>
    </Button>
  );
}

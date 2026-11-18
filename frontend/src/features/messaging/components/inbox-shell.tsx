"use client";

import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import { ConversationList } from "./conversation-list";

/**
 * Airbnb-style inbox. Desktop: the conversation list beside the open thread.
 * Mobile: `/messages` shows the list and `/messages/[id]` the thread, one at a time.
 */
export function InboxShell({ children }: { children: React.ReactNode }) {
  const t = useTranslations("messaging");
  const pathname = usePathname();
  const match = /^\/messages\/([^/]+)/.exec(pathname);
  const selectedId = match?.[1] ?? null;

  return (
    <div className="grid h-[calc(100dvh-9rem)] min-h-[30rem] overflow-hidden rounded-3xl border bg-card shadow-card lg:grid-cols-[22rem_minmax(0,1fr)]">
      <aside
        aria-label={t("listLabel")}
        className={cn("min-h-0 flex-col border-r", selectedId ? "hidden lg:flex" : "flex")}
      >
        <ConversationList selectedId={selectedId} />
      </aside>
      <section className={cn("min-h-0 min-w-0 flex-col", selectedId ? "flex" : "hidden lg:flex")}>{children}</section>
    </div>
  );
}

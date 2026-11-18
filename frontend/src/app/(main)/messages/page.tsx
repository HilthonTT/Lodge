import { MessagesSquare } from "lucide-react";
import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("messaging");
  return { title: t("title") };
}

/** Right pane on desktop before a conversation is picked (hidden on mobile). */
export default async function MessagesPage() {
  const t = await getTranslations("messaging");
  return (
    <div className="grid flex-1 place-items-center p-8 text-center">
      <div className="grid max-w-sm justify-items-center gap-3">
        <span className="grid size-14 place-items-center rounded-2xl bg-accent text-accent-foreground">
          <MessagesSquare className="size-6" aria-hidden="true" />
        </span>
        <h2 className="text-lg font-semibold">{t("pickTitle")}</h2>
        <p className="text-sm text-muted-foreground">{t("pickDescription")}</p>
      </div>
    </div>
  );
}

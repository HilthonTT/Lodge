"use client";

import { Share2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";

export function ShareButton({ title }: { title: string }) {
  const t = useTranslations("apartment");

  const onShare = async () => {
    const url = window.location.href;
    try {
      if (navigator.share) {
        await navigator.share({ title, url });
        return;
      }
      await navigator.clipboard.writeText(url);
      toast.success(t("linkCopied"));
    } catch (error) {
      if ((error as DOMException)?.name !== "AbortError") toast.error(t("shareFailed"));
    }
  };

  return (
    <Button variant="ghost" className="rounded-full underline-offset-4 hover:underline" onClick={onShare}>
      <Share2 aria-hidden="true" /> {t("share")}
    </Button>
  );
}

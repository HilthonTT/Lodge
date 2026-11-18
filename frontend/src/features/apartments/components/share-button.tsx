"use client";

import { Share2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";

export function ShareButton({ title }: { title: string }) {
  const onShare = async () => {
    const url = window.location.href;
    try {
      if (navigator.share) {
        await navigator.share({ title, url });
        return;
      }
      await navigator.clipboard.writeText(url);
      toast.success("Link copied to clipboard");
    } catch (error) {
      if ((error as DOMException)?.name !== "AbortError") toast.error("Couldn't share this listing");
    }
  };

  return (
    <Button variant="ghost" className="rounded-full underline-offset-4 hover:underline" onClick={onShare}>
      <Share2 aria-hidden="true" /> Share
    </Button>
  );
}

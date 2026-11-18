"use client";

import { Check, Copy } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";

type CopyButtonProps = {
  value: string;
  label?: string;
  size?: "sm" | "default" | "icon-sm";
  variant?: "outline" | "ghost" | "secondary";
  className?: string;
};

/** Copies `value` to the clipboard and briefly confirms it. */
export function CopyButton({ value, label, size = "sm", variant = "outline", className }: CopyButtonProps) {
  const t = useTranslations("common");
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied) return;
    const timer = window.setTimeout(() => setCopied(false), 2000);
    return () => window.clearTimeout(timer);
  }, [copied]);

  const onCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
    } catch {
      toast.error(t("copyFailed"));
    }
  };

  const text = copied ? t("copied") : (label ?? t("copy"));
  return (
    <Button
      type="button"
      size={size}
      variant={variant}
      className={className}
      onClick={onCopy}
      aria-label={size === "icon-sm" ? text : undefined}
    >
      {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
      {size !== "icon-sm" && text}
      <span className="sr-only" aria-live="polite">
        {copied ? t("copied") : ""}
      </span>
    </Button>
  );
}

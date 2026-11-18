"use client";

import { SendHorizontal } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useLayoutEffect, useRef, useState } from "react";

import { Button } from "@/components/ui/button";
import { formatNumber } from "@/lib/format";
import { cn } from "@/lib/utils";

import { MESSAGE_MAX_LENGTH } from "../api/messaging";

/** Show the counter once the message gets close to the limit. */
const COUNTER_FROM = MESSAGE_MAX_LENGTH - 300;

/** Message box: Enter sends, Shift+Enter adds a line; grows up to ~8 lines. */
export function Composer({
  onSend,
  disabled,
  placeholder,
}: {
  onSend: (body: string) => void;
  disabled?: boolean;
  placeholder?: string;
}) {
  const t = useTranslations("messaging.composer");
  const locale = useLocale();
  const [value, setValue] = useState("");
  const ref = useRef<HTMLTextAreaElement>(null);
  const trimmed = value.trim();
  const tooLong = value.length > MESSAGE_MAX_LENGTH;
  const canSend = trimmed.length > 0 && !tooLong && !disabled;

  // Auto-grow.
  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 200)}px`;
  }, [value]);

  const send = () => {
    if (!canSend) return;
    onSend(trimmed);
    setValue("");
    ref.current?.focus();
  };

  return (
    <form
      className="border-t bg-card p-3 sm:p-4"
      onSubmit={(e) => {
        e.preventDefault();
        send();
      }}
    >
      <div className="flex items-end gap-2 rounded-2xl border bg-background p-2 shadow-xs focus-within:ring-[3px] focus-within:ring-ring/25">
        <label htmlFor="composer" className="sr-only">
          {t("label")}
        </label>
        <textarea
          id="composer"
          ref={ref}
          rows={1}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
              e.preventDefault();
              send();
            }
          }}
          placeholder={placeholder ?? t("placeholder")}
          aria-describedby="composer-hint"
          aria-invalid={tooLong || undefined}
          className="max-h-[200px] min-h-10 flex-1 resize-none bg-transparent px-2 py-2 text-sm leading-relaxed outline-none placeholder:text-muted-foreground"
          data-testid="composer-input"
        />
        <Button
          type="submit"
          size="icon"
          className="shrink-0 rounded-full"
          disabled={!canSend}
          aria-label={t("send")}
          data-testid="composer-send"
        >
          <SendHorizontal aria-hidden="true" />
        </Button>
      </div>
      <div id="composer-hint" className="mt-1.5 flex justify-between gap-2 px-1 text-xs text-muted-foreground">
        <span className="hidden sm:inline">{t("hint")}</span>
        {value.length >= COUNTER_FROM && (
          <span className={cn("ml-auto tabular-nums", tooLong && "font-medium text-destructive")} aria-live="polite">
            {t("counter", { used: formatNumber(value.length, locale), max: formatNumber(MESSAGE_MAX_LENGTH, locale) })}
          </span>
        )}
      </div>
    </form>
  );
}

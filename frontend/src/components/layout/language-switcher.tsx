"use client";

import { Check, Globe, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useState, useTransition } from "react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { setLocale } from "@/i18n/actions";
import { LOCALE_NAMES, LOCALES, type Locale } from "@/i18n/config";
import { cn } from "@/lib/utils";

type LanguageSwitcherProps = {
  /** "icon": round globe button (navbar). "compact": globe + language name (footer). */
  variant?: "icon" | "compact";
  className?: string;
};

/**
 * Airbnb-style language picker: a globe button that opens a dialog listing every
 * language in its own name. Choosing one stores it in the `NEXT_LOCALE` cookie
 * and re-renders the current route; the URL doesn't change.
 */
export function LanguageSwitcher({ variant = "icon", className }: LanguageSwitcherProps) {
  const t = useTranslations("language");
  const locale = useLocale();
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState<Locale | null>(null);
  const [isPending, startTransition] = useTransition();

  const choose = (next: Locale) => {
    if (next === locale) {
      setOpen(false);
      return;
    }
    setPending(next);
    startTransition(async () => {
      await setLocale(next);
      router.refresh();
      setOpen(false);
      setPending(null);
    });
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {variant === "icon" ? (
          <Button
            variant="ghost"
            size="icon"
            className={cn("rounded-full", className)}
            aria-label={`${t("choose")} (${t("current", { language: LOCALE_NAMES[locale] })})`}
          >
            <Globe className="size-[1.15rem]" aria-hidden="true" />
          </Button>
        ) : (
          <Button
            variant="ghost"
            size="sm"
            className={cn("-ml-2 h-8 gap-1.5 px-2 text-xs font-medium text-foreground", className)}
            aria-label={`${t("choose")} (${t("current", { language: LOCALE_NAMES[locale] })})`}
          >
            <Globe className="size-3.5" aria-hidden="true" />
            {LOCALE_NAMES[locale]}
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>{t("choose")}</DialogTitle>
          <DialogDescription>{t("description")}</DialogDescription>
        </DialogHeader>
        <ul className="grid gap-2 sm:grid-cols-3" aria-busy={isPending}>
          {LOCALES.map((option) => {
            const active = option === locale;
            return (
              <li key={option}>
                <button
                  type="button"
                  lang={option}
                  onClick={() => choose(option)}
                  disabled={isPending}
                  aria-current={active ? "true" : undefined}
                  className={cn(
                    "flex w-full items-start justify-between gap-2 rounded-xl border px-4 py-3 text-left transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none disabled:cursor-wait",
                    active ? "border-foreground" : "border-transparent hover:bg-muted",
                  )}
                >
                  <span className="grid gap-0.5">
                    <span className="text-sm font-medium">{LOCALE_NAMES[option]}</span>
                    {/* The language's name in the current UI language, e.g. "Dutch". */}
                    <span lang={locale} className="text-xs text-muted-foreground">
                      {t(`names.${option}`)}
                    </span>
                  </span>
                  {pending === option ? (
                    <Loader2 className="mt-0.5 size-4 animate-spin text-muted-foreground" aria-hidden="true" />
                  ) : (
                    active && <Check className="mt-0.5 size-4" aria-hidden="true" />
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      </DialogContent>
    </Dialog>
  );
}

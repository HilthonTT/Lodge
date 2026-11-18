"use client";

import { Check, Globe, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useState, useTransition } from "react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useCurrency } from "@/features/payments/currency-provider";
import { setLocale } from "@/i18n/actions";
import { INTL_LOCALES, LOCALE_NAMES, LOCALES, type Locale } from "@/i18n/config";
import { CURRENCIES, type DisplayCurrency } from "@/lib/currency";
import { cn } from "@/lib/utils";

type LanguageSwitcherProps = {
  /** "icon": round globe button (navbar). "compact": globe + language name (footer). */
  variant?: "icon" | "compact";
  className?: string;
};

const optionClass =
  "flex w-full items-start justify-between gap-2 rounded-xl border px-4 py-3 text-left transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none disabled:cursor-wait";

/** "€" / "$" / "CHF" as the locale writes the currency on its own. */
function currencySymbol(currency: string, locale: string) {
  try {
    return (
      new Intl.NumberFormat(locale, { style: "currency", currency, currencyDisplay: "narrowSymbol" })
        .formatToParts(0)
        .find((part) => part.type === "currency")?.value ?? currency
    );
  } catch {
    return currency;
  }
}

function currencyName(currency: string, locale: string) {
  try {
    return new Intl.DisplayNames([locale], { type: "currency" }).of(currency) ?? currency;
  } catch {
    return currency;
  }
}

/**
 * Airbnb-style "language and currency" picker: a globe button that opens a dialog
 * with two tabs. Languages are listed in their own names; choosing one stores it
 * in the `NEXT_LOCALE` cookie and re-renders the route (the URL doesn't change).
 * The currency tab picks the display currency for prices (`LODGE_CURRENCY` cookie).
 */
export function LanguageSwitcher({ variant = "icon", className }: LanguageSwitcherProps) {
  const t = useTranslations("language");
  const locale = useLocale();
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState<Locale | null>(null);
  const [isPending, startTransition] = useTransition();
  const { currency, setCurrency } = useCurrency();
  const intlLocale = INTL_LOCALES[locale as Locale] ?? locale;
  const triggerLabel = `${t("choose")} (${t("current", { language: LOCALE_NAMES[locale as Locale] ?? locale })}, ${currency})`;

  const chooseCurrency = (next: DisplayCurrency) => {
    setCurrency(next);
    setOpen(false);
    // Server-rendered labels (e.g. the price filter chip) read the cookie.
    router.refresh();
  };

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
            aria-label={triggerLabel}
          >
            <Globe className="size-[1.15rem]" aria-hidden="true" />
          </Button>
        ) : (
          <Button
            variant="ghost"
            size="sm"
            className={cn("-ml-2 h-8 gap-1.5 px-2 text-xs font-medium text-foreground", className)}
            aria-label={triggerLabel}
          >
            <Globe className="size-3.5" aria-hidden="true" />
            {LOCALE_NAMES[locale as Locale]}
            <span className="text-muted-foreground" aria-hidden="true">
              · {currencySymbol(currency, intlLocale)} {currency}
            </span>
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-xl">
        <Tabs defaultValue="language" className="grid gap-5">
          <DialogHeader>
            <DialogTitle className="sr-only">{t("dialogTitle")}</DialogTitle>
            <TabsList className="justify-self-start">
              <TabsTrigger value="language">{t("tabLanguage")}</TabsTrigger>
              <TabsTrigger value="currency">{t("tabCurrency")}</TabsTrigger>
            </TabsList>
          </DialogHeader>
          <TabsContent value="language" className="grid gap-4">
            <div className="grid gap-1">
              <h3 className="text-lg font-semibold">{t("choose")}</h3>
              <DialogDescription>{t("description")}</DialogDescription>
            </div>
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
                      className={cn(optionClass, active ? "border-foreground" : "border-transparent hover:bg-muted")}
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
          </TabsContent>
          <TabsContent value="currency" className="grid gap-4">
            <div className="grid gap-1">
              <h3 className="text-lg font-semibold">{t("chooseCurrency")}</h3>
              <p className="text-sm text-muted-foreground">{t("currencyDescription")}</p>
            </div>
            <ul className="grid gap-2 sm:grid-cols-3">
              {CURRENCIES.map((code) => {
                const active = code === currency;
                return (
                  <li key={code}>
                    <button
                      type="button"
                      onClick={() => chooseCurrency(code)}
                      aria-current={active ? "true" : undefined}
                      data-currency={code}
                      className={cn(optionClass, active ? "border-foreground" : "border-transparent hover:bg-muted")}
                    >
                      <span className="grid gap-0.5">
                        <span className="text-sm font-medium">{currencyName(code, intlLocale)}</span>
                        <span className="text-xs text-muted-foreground">
                          {code} – {currencySymbol(code, intlLocale)}
                        </span>
                      </span>
                      {active && <Check className="mt-0.5 size-4" aria-hidden="true" />}
                    </button>
                  </li>
                );
              })}
            </ul>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}

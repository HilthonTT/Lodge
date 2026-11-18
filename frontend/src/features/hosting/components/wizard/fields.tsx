"use client";

import { Minus, Plus } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { formatNumber } from "@/lib/format";

/** +/- counter with any step (bathrooms go in halves). */
export function NumberStepper({
  id,
  label,
  description,
  value,
  min,
  max,
  step = 1,
  onChange,
}: {
  id: string;
  label: string;
  description?: string;
  value: number;
  min: number;
  max: number;
  step?: number;
  onChange: (value: number) => void;
}) {
  const t = useTranslations("common");
  const locale = useLocale();
  const safe = Number.isFinite(value) ? value : min;
  return (
    <div className="flex items-center justify-between gap-4 py-4" role="group" aria-labelledby={`${id}-label`}>
      <div className="grid gap-0.5">
        <span id={`${id}-label`} className="font-medium">
          {label}
        </span>
        {description && <span className="text-sm text-muted-foreground">{description}</span>}
      </div>
      <div className="flex items-center gap-3">
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.max(min, safe - step))}
          disabled={safe <= min}
          aria-label={t("decrease", { label })}
        >
          <Minus />
        </Button>
        <output id={id} aria-live="polite" className="w-8 text-center font-semibold tabular-nums">
          {formatNumber(safe, locale)}
        </output>
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.min(max, safe + step))}
          disabled={safe >= max}
          aria-label={t("increase", { label })}
        >
          <Plus />
        </Button>
      </div>
    </div>
  );
}

/** Labelled switch row. */
export function SwitchRow({
  id,
  label,
  description,
  checked,
  onCheckedChange,
}: {
  id: string;
  label: string;
  description?: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-4 py-4">
      <label htmlFor={id} className="grid gap-0.5">
        <span className="font-medium">{label}</span>
        {description && <span className="text-sm text-muted-foreground">{description}</span>}
      </label>
      <Switch id={id} checked={checked} onCheckedChange={onCheckedChange} />
    </div>
  );
}

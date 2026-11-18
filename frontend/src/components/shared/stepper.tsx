"use client";

import { Minus, Plus } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";

type StepperProps = {
  id: string;
  label: string;
  description?: string;
  value: number;
  min?: number;
  max?: number;
  onChange: (value: number) => void;
};

/** Accessible +/- counter (guests, rooms). */
export function Stepper({ id, label, description, value, min = 1, max = 16, onChange }: StepperProps) {
  const t = useTranslations("common");
  return (
    <div className="flex items-center justify-between gap-4" role="group" aria-labelledby={`${id}-label`}>
      <div className="grid gap-0.5">
        <span id={`${id}-label`} className="text-sm font-medium">
          {label}
        </span>
        {description && <span className="text-xs text-muted-foreground">{description}</span>}
      </div>
      <div className="flex items-center gap-3">
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.max(min, value - 1))}
          disabled={value <= min}
          aria-label={t("decrease", { label })}
        >
          <Minus />
        </Button>
        <output aria-live="polite" className="w-6 text-center text-sm font-semibold tabular-nums">
          {value}
        </output>
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          onClick={() => onChange(Math.min(max, value + 1))}
          disabled={value >= max}
          aria-label={t("increase", { label })}
        >
          <Plus />
        </Button>
      </div>
    </div>
  );
}
